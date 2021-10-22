using Barebones.Networking;
using Mirror;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class RoomServerBehaviour : MsfBaseClientModule
    {
        protected Dictionary<int, RoomPlayer> roomPlayersByMsfPeerId;
        protected Dictionary<int, RoomPlayer> roomPlayersByMirrorPeerId;
        protected Dictionary<string, RoomPlayer> roomPlayersByUsername;

        /// <summary>
        /// Just an info about current connection process
        /// </summary>
        private bool isConnecting = false;

        /// <summary>
        /// Options of this room we must share with clients
        /// </summary>
        private RoomOptions roomOptions;

        [Header("Room server Settings")]
        [SerializeField, Range(1, 500)]
        private int defaultMaxConnections = 100;

        [Header("Master Connection Settings"), SerializeField]
        private string masterIp = "127.0.0.1";
        [SerializeField]
        private int masterPort = 5000;

        [Header("Editor Settings"), SerializeField]
        private HelpBox hpEditor = new HelpBox()
        {
            Text = "Editor settings are used only while running in editor and for test purpose only",
            Type = HelpBoxType.Warning
        };

        [SerializeField]
        protected bool autoStartInEditor = true;

        /// <summary>
        /// Controller of the room
        /// </summary>
        public RoomController CurrentRoomController { get; private set; }

        /// <summary>
        /// Fires when server room is successfully registered
        /// </summary>
        public UnityEvent OnRoomServerRegisteredEvent;

        /// <summary>
        /// Fires when new playerjoined room
        /// </summary>
        public event Action<RoomPlayer> OnPlayerJoinedRoomEvent;

        /// <summary>
        /// Fires when existing player left room
        /// </summary>
        public event Action<RoomPlayer> OnPlayerLeftRoomEvent;

        protected virtual void OnApplicationQuit()
        {
            if (Connection != null)
                Connection.Disconnect();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unregister handlers
            NetworkServer.UnregisterHandler<ValidateRoomAccessRequestMessage>();
        }

        /// <summary>
        /// Let's set up all the options of room server before connection
        /// </summary>
        protected override void OnBeforeClientConnectedToServer()
        {
            // In case we are nested to another object
            transform.SetParent(null);

            roomPlayersByMsfPeerId = new Dictionary<int, RoomPlayer>();
            roomPlayersByMirrorPeerId = new Dictionary<int, RoomPlayer>();
            roomPlayersByUsername = new Dictionary<string, RoomPlayer>();

            // Register handler to listen to client access validation request
            NetworkServer.RegisterHandler<ValidateRoomAccessRequestMessage>(ValidateRoomAccessRequestHandler, false);

            // If room port is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.RoomPort))
            {
                SetPort((ushort)Msf.Args.RoomPort);
            }

            // If master IP is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.MasterIp))
            {
                masterIp = Msf.Args.MasterIp;
            }

            // If master port is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.MasterPort))
            {
                masterPort = Msf.Args.MasterPort;
            }

            // Start room server at start in editor
            // Just for test purpose only
            if (Msf.Runtime.IsEditor && autoStartInEditor && !Msf.Options.Has(MsfDictKeys.autoStartRoomClient))
            {
                StartRoomServer(true);
            }

            // Start room server at start
            if (Msf.Args.StartClientConnection)
            {
                StartRoomServer();
            }
        }

        /// <summary>
        /// Let's create new connection to master server
        /// </summary>
        /// <returns></returns>
        protected override IClientSocket ConnectionFactory()
        {
            return Msf.Create.ClientSocket();
        }

        /// <summary>
        /// When the socket is changed we also need to change it in all the services we want to use
        /// </summary>
        /// <param name="socket"></param>
        protected override void OnConnectionSocketChanged(IClientSocket socket)
        {
            Msf.Server.Rooms.ChangeConnection(socket);
            Msf.Server.Spawners.ChangeConnection(socket);
            Msf.Server.Auth.ChangeConnection(socket);
        }

        /// <summary>
        /// Starting connection to master server as client to be able to register room later after successful connection
        /// </summary>
        private void StartConnectionToMaster()
        {
            // If connection process is started
            if (!isConnecting)
                logger.Info($"Connecting room server to master server at: {masterIp}:{masterPort}");
            else
                logger.Info($"Retrying connection of room server to master server at: {masterIp}:{masterPort}");

            // Set connection process checker as TRUE :)
            isConnecting = true;

            // Start client connection
            Connection.Connect(masterIp, masterPort);

            // Wait a result of client connection
            Connection.WaitForConnection((clientSocket) =>
            {
                if (!clientSocket.IsConnected)
                {
                    StartConnectionToMaster();
                }
                else
                {
                    isConnecting = false;
                    logger.Info("Room server is successfuly connected to master server");
                    logger.Info("Starting Mirror server");
                }
            }, 2f);
        }

        /// <summary>
        /// Start room server
        /// </summary>
        /// <param name="ignoreForceClientMode"></param>
        public void StartRoomServer(bool ignoreForceClientMode = false)
        {
            if (Msf.Client.Rooms.ForceClientMode && !ignoreForceClientMode) return;

            // Setup room options
            roomOptions = SetupRoomOptions();

            MsfTimer.WaitForSeconds(1f, () =>
            {
                if (!Connection.IsConnected)
                {
                    // Start the connection to master
                    StartConnectionToMaster();
                }
            });
        }

        /// <summary>
        /// Set network transport port
        /// </summary>
        /// <param name="port"></param>
        public virtual void SetPort(ushort port)
        {
            ((TelepathyTransport)Transport.activeTransport).port = port;
        }

        /// <summary>
        /// Get network transport port
        /// </summary>
        /// <returns></returns>
        public virtual ushort GetPort()
        {
            return ((TelepathyTransport)Transport.activeTransport).port;
        }

        /// <summary>
        /// Fires when client that wants to connect to this room made request to validate the access token
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void ValidateRoomAccessRequestHandler(NetworkConnection conn, ValidateRoomAccessRequestMessage msg)
        {
            logger.Debug($"Room client {conn.connectionId} asked to validate access token [{msg.Token}]");

            // Triying to validate given token
            Msf.Server.Rooms.ValidateAccess(CurrentRoomController.RoomId, msg.Token, (usernameAndPeerId, error) =>
            {
                // If token is not valid
                if (usernameAndPeerId == null)
                {
                    logger.Error(error);

                    conn.Send(new RoomAccessValidationResultMessage()
                    {
                        Error = error,
                        Status = ResponseStatus.Failed
                    });

                    MsfTimer.WaitForSeconds(1f, () => conn.Disconnect());

                    return;
                }

                logger.Debug($"Client {conn.connectionId} is successfully validated");

                // Send success result to room client
                conn.Send(new RoomAccessValidationResultMessage()
                {
                    Error = string.Empty,
                    Status = ResponseStatus.Success
                });

                logger.Debug("Getting his account info...");

                Msf.Server.Auth.GetPeerAccountInfo(usernameAndPeerId.PeerId, (accountInfo, accountError) =>
                {
                    if (accountInfo == null)
                    {
                        logger.Error(accountError);

                        conn.Send(new GetPeerAccountInfoResultMessage()
                        {
                            Error = accountError,
                            Status = ResponseStatus.Error
                        });

                        MsfTimer.WaitForSeconds(1f, () => conn.Disconnect());

                        return;
                    }

                    logger.Debug($"Client {conn.connectionId} has become a player of this room. Congratulations to {accountInfo.Username}");

                    conn.Send(new GetPeerAccountInfoResultMessage()
                    {
                        Error = string.Empty,
                        Status = ResponseStatus.Success
                    });

                    var player = new RoomPlayer(usernameAndPeerId.PeerId, conn, accountInfo.Username, accountInfo.CustomOptions);

                    roomPlayersByMsfPeerId.Add(usernameAndPeerId.PeerId, player);
                    roomPlayersByMirrorPeerId.Add(conn.connectionId, player);
                    roomPlayersByUsername.Add(accountInfo.Username, player);

                    OnPlayerJoinedRoomEvent?.Invoke(player);
                });
            });
        }

        /// <summary>
        /// Setup this room options
        /// </summary>
        /// <returns></returns>
        public virtual RoomOptions SetupRoomOptions()
        {
            return new RoomOptions
            {
                IsPublic = Msf.Args.RoomIsPrivate,
                MaxConnections = Msf.Args.RoomMaxConnections,
                Name = Msf.Args.RoomName,
                Password = Msf.Args.RoomPassword,
                RoomIp = Msf.Args.RoomIp,
                RoomPort = Msf.Args.ExtractValueInt(Msf.Args.Names.RoomPort, GetPort()),
                Region = Msf.Args.RoomRegion
            };
        }

        /// <summary>
        /// Fired when this room server is connected to master as client
        /// </summary>
        protected override void OnClientConnectedToServer()
        {
            MsfTimer.WaitForEndOfFrame(() =>
            {
                // Start the server on next frame
                // Set max number of connections
                NetworkManager.singleton.maxConnections = roomOptions.MaxConnections <= 0 ? defaultMaxConnections : roomOptions.MaxConnections;

                // Start mirror server
                NetworkManager.singleton.StartServer();

                // Before we register our room we need to setup everything
                BeforeRoomServerRegistering();
            });
        }

        /// <summary>
        /// Fired when this room server is disconnected from master as client
        /// </summary>
        protected override void OnClientDisconnectedFromServer()
        {
            // Quit the room
            Msf.Runtime.Quit();
        }

        /// <summary>
        /// Before we register our room we need to setup everything
        /// </summary>
        protected virtual void BeforeRoomServerRegistering()
        {
            if (Msf.Server.Spawners.IsSpawnedProccess)
            {
                Msf.Server.Spawners.RegisterSpawnedProcess(Msf.Args.SpawnTaskId, Msf.Args.SpawnTaskUniqueCode, (taskController, error) =>
                {
                    if (taskController == null)
                    {
                        logger.Error($"Room server process cannot be registered. The reason is: {error}");
                        return;
                    }

                    // Then start registering our room server
                    RegisterRoomServer(() =>
                    {
                        logger.Info("Finalizing registration task");
                        taskController.FinalizeTask(new Dictionary<string, string>(), () =>
                        {
                            logger.Info("Ok!");
                            OnRoomServerRegisteredEvent?.Invoke();
                        });
                    });
                });
            }
            else
            {
                RegisterRoomServer(() =>
                {
                    logger.Info("Ok!");
                    OnRoomServerRegisteredEvent?.Invoke();
                });
            }
        }

        /// <summary>
        /// Start registering our room server
        /// </summary>
        protected virtual void RegisterRoomServer(UnityAction successCallback = null)
        {
            Msf.Server.Rooms.RegisterRoom(roomOptions, (controller, error) =>
            {
                if (controller == null)
                {
                    logger.Error(error);
                    return;
                }

                CurrentRoomController = controller;

                logger.Info($"Room Created successfully. Room ID: {controller.RoomId}, {roomOptions}");

                successCallback?.Invoke();
            });
        }

        /// <summary>
        /// Fires when mirror client disconnected
        /// </summary>
        /// <param name="mirrorConn"></param>
        public void OnPlayerLeftRoomHandler(NetworkConnection mirrorConn)
        {
            if (roomPlayersByMirrorPeerId.TryGetValue(mirrorConn.connectionId, out RoomPlayer player))
            {
                logger.Debug($"Room server player {player.Username} with room client Id {mirrorConn.connectionId} left the room");

                roomPlayersByMirrorPeerId.Remove(player.MirrorPeer.connectionId);
                roomPlayersByMsfPeerId.Remove(player.MsfPeerId);
                roomPlayersByUsername.Remove(player.Username);

                // Notify master server about disconnected player
                CurrentRoomController.NotifyPlayerLeft(player.MsfPeerId);

                OnPlayerLeftRoomEvent?.Invoke(player);

            }
            else
            {
                logger.Debug($"Room server client {mirrorConn.connectionId} left the room");
            }
        }
    }
}