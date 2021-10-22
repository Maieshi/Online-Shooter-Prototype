using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.MasterServer.Examples.BasicSpawnerMirror;
using Barebones.Networking;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Barebones.Bridges.Mirror
{
    public class MirrorRoomServer : MsfBaseClientModule
    {
        #region INSPECTOR

        [Header("Master Connection Settings"), SerializeField]
        private string masterIp = "127.0.0.1";
        [SerializeField]
        private int masterPort = 5000;

        [SerializeField]
        protected bool autoStartInEditor = true;

        #endregion

        /// <summary>
        /// List of players filtered by MSF peer Id
        /// </summary>
        protected Dictionary<int, MirrorRoomPlayer> roomPlayersByMsfPeerId;

        /// <summary>
        /// List of players filtered by Mirror peer Id
        /// </summary>
        protected Dictionary<int, MirrorRoomPlayer> roomPlayersByMirrorPeerId;

        /// <summary>
        /// List of players filtered by username
        /// </summary>
        protected Dictionary<string, MirrorRoomPlayer> roomPlayersByUsername;

        /// <summary>
        /// Options of this room we must share with clients
        /// </summary>
        private RoomOptions roomOptions;

        /// <summary>
        /// Room manager
        /// </summary>
        public MirrorRoomManager RoomManager { get; set; }

        /// <summary>
        /// Controller of the room
        /// </summary>
        public RoomController CurrentRoomController { get; private set; }

        /// <summary>
        /// Fires when server room is successfully registered
        /// </summary>
        public Action OnRoomServerRegisteredEvent;

        /// <summary>
        /// Fires when new playerjoined room
        /// </summary>
        public event Action<MirrorRoomPlayer> OnPlayerJoinedRoomEvent;

        /// <summary>
        /// Fires when existing player left room
        /// </summary>
        public event Action<MirrorRoomPlayer> OnPlayerLeftRoomEvent;

        protected override void Awake()
        {
            base.Awake();

            // Init logger
            logger = Msf.Create.Logger(GetType().Name);
            logger.LogLevel = logLevel;

            // Create filtered lists of players
            roomPlayersByMsfPeerId = new Dictionary<int, MirrorRoomPlayer>();
            roomPlayersByMirrorPeerId = new Dictionary<int, MirrorRoomPlayer>();
            roomPlayersByUsername = new Dictionary<string, MirrorRoomPlayer>();

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
        }

        protected override void OnBeforeClientConnectedToServer()
        {
            // Register handler to listen to client access validation request
            NetworkServer.RegisterHandler<ValidateRoomAccessRequestMessage>(ValidateRoomAccessRequestHandler, false);

            if (Msf.Runtime.IsEditor && autoStartInEditor && !Msf.Options.Has(MsfDictKeys.autoStartRoomClient))
            {
                MsfTimer.WaitForEndOfFrame(() => {
                    StartRoomServer(true);
                });
            }

            // Start room server at start
            if (Msf.Args.StartClientConnection && !Msf.Runtime.IsEditor)
            {
                MsfTimer.WaitForEndOfFrame(() => {
                    StartRoomServer();
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            if (Connection != null)
                Connection.Disconnect();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unregister handlers
            NetworkServer.UnregisterHandler<ValidateRoomAccessRequestMessage>();
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
        /// Set this room options
        /// </summary>
        /// <returns></returns>
        protected virtual RoomOptions SetRoomOptions()
        {
            return new RoomOptions
            {
                IsPublic = Msf.Args.RoomIsPrivate,
                MaxConnections = RoomManager.maxConnections,
                Name = Msf.Args.RoomName,
                Password = Msf.Args.RoomPassword,
                RoomIp = Msf.Args.RoomIp,
                RoomPort = Msf.Args.ExtractValueInt(Msf.Args.Names.RoomPort, RoomManager.GetPort()),
                Region = Msf.Args.RoomRegion
            };
        }

        /// <summary>
        /// Starting connection to master server as client to be able to register room later after successful connection
        /// </summary>
        private void ConnectToMaster()
        {
            // Start client connection
            if (!Connection.IsConnected)
            {
                Connection.Connect(masterIp, masterPort);
            }

            // Wait a result of client connection
            Connection.WaitForConnection((clientSocket) =>
            {
                if (!clientSocket.IsConnected)
                {
                    logger.Error("Failed to connect room server to master server");
                }
                else
                {
                    logger.Info("Room server is successfuly connected to master server");
                    logger.Info("Starting Mirror server...");

                    // Start Mirror server
                    RoomManager.StartServer();
                }
            }, 4f);
        }

        /// <summary>
        /// Fired when this room server is disconnected from master as client
        /// </summary>
        protected override void OnClientDisconnectedFromServer()
        {
            // Stop Mirror server
            RoomManager.StopServer();

            // Quit the room
            Msf.Runtime.Quit();
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
                logger.Debug("Getting his account info...");

                Msf.Server.Auth.GetPeerAccountInfo(usernameAndPeerId.PeerId, (accountInfo, accountError) =>
                {
                    if (accountInfo == null)
                    {
                        logger.Error(accountError);

                        conn.Send(new RoomAccessValidationResultMessage()
                        {
                            Error = accountError,
                            Status = ResponseStatus.Error
                        });

                        MsfTimer.WaitForSeconds(1f, () => conn.Disconnect());

                        return;
                    }

                    logger.Debug($"Client {conn.connectionId} has become a player of this room. Congratulations to {accountInfo.Username}");

                    conn.Send(new RoomAccessValidationResultMessage()
                    {
                        Error = string.Empty,
                        Status = ResponseStatus.Success
                    });

                    // Create new room player
                    var player = new MirrorRoomPlayer(usernameAndPeerId.PeerId, conn, accountInfo.Username, accountInfo.CustomOptions);

                    // Add this player to filtered lists
                    roomPlayersByMsfPeerId.Add(usernameAndPeerId.PeerId, player);
                    roomPlayersByMirrorPeerId.Add(conn.connectionId, player);
                    roomPlayersByUsername.Add(accountInfo.Username, player);

                    // Inform subscribers about this player
                    OnPlayerJoinedRoomEvent?.Invoke(player);
                });
            });
        }

        /// <summary>
        /// Before we register our room we need to register spawned process if required
        /// </summary>
        protected void RegisterSpawnedProcess()
        {
            // Let's register this process
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
        /// Start room server
        /// </summary>
        /// <param name="ignoreForceClientMode"></param>
        public virtual void StartRoomServer(bool ignoreForceClientMode = false)
        {
            if (Msf.Client.Rooms.ForceClientMode && !ignoreForceClientMode) return;

            // Set connection of the room server
            Connection = ConnectionFactory();

            // Set this connection to services we want to use
            Msf.Server.Rooms.ChangeConnection(Connection);
            Msf.Server.Spawners.ChangeConnection(Connection);
            Msf.Server.Auth.ChangeConnection(Connection);
            Msf.Server.Profiles.ChangeConnection(Connection);

            // Set room oprions
            roomOptions = SetRoomOptions();

            // Start connecting room server to master server
            ConnectToMaster();
        }

        /// <summary>
        /// Fired when mirror server is started
        /// </summary>
        public void OnMirrorServerStarted()
        {
            // If this room was spawned
            if (Msf.Server.Spawners.IsSpawnedProccess)
            {
                // Try to register spawned process first
                RegisterSpawnedProcess();
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
        /// Fired when mirror client disconnected
        /// </summary>
        /// <param name="mirrorConn"></param>
        public void OnPlayerLeftRoomHandler(NetworkConnection mirrorConn)
        {
            // Try to find player in filtered list
            if (roomPlayersByMirrorPeerId.TryGetValue(mirrorConn.connectionId, out MirrorRoomPlayer player))
            {
                logger.Debug($"Room server player {player.Username} with room client Id {mirrorConn.connectionId} left the room");

                // Remove thisplayer from filtered list
                roomPlayersByMirrorPeerId.Remove(player.MirrorPeer.connectionId);
                roomPlayersByMsfPeerId.Remove(player.MsfPeerId);
                roomPlayersByUsername.Remove(player.Username);

                // Notify master server about disconnected player
                CurrentRoomController.NotifyPlayerLeft(player.MsfPeerId);

                // Inform subscribers about this bad guy
                OnPlayerLeftRoomEvent?.Invoke(player);

            }
            else
            {
                logger.Debug($"Room server client {mirrorConn.connectionId} left the room");
            }
        }
    }
}