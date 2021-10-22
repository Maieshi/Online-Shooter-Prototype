using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Barebones.Bridges.Mirror
{
    public class MirrorRoomClient : MsfBaseClientModule
    {
        #region INSPECTOR

        /// <summary>
        /// Room access that client gets from master server
        /// </summary>
        protected RoomAccessPacket roomAccess;

        [Header("Master Connection Settings")]
        [SerializeField]
        private string masterIp = "127.0.0.1";
        [SerializeField]
        private int masterPort = 5000;

        [SerializeField]
        protected bool autoStartInEditor = true;

        [SerializeField]
        protected bool signInAsGuest = true;

        [SerializeField]
        protected string username = "qwerty";

        [SerializeField]
        protected string password = "qwerty12345";

        #endregion

        /// <summary>
        /// Fires when room server has given an access to us
        /// </summary>
        public event Action OnAccessGrantedEvent;

        /// <summary>
        /// Fires when room server has rejected an access to us
        /// </summary>
        public event Action OnAccessDiniedEvent;

        /// <summary>
        /// Fire when room server sends alert message to client
        /// </summary>
        public event Action<RoomAlertMessage> OnRoomAlertMessageEvent;

        /// <summary>
        /// Fires when client joined the room as player
        /// </summary>
        public event Action OnJoinedRoomEvent;

        /// <summary>
        /// Room manager
        /// </summary>
        public MirrorRoomManager RoomManager { get; set; }

        protected override void Awake()
        {
            base.Awake();

            logger = Msf.Create.Logger(GetType().Name);
            logger.LogLevel = logLevel;

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
            NetworkClient.RegisterHandler<RoomAccessValidationResultMessage>(RoomAccessValidationResultMessageHandler, false);
            NetworkClient.RegisterHandler<RoomAlertMessage>(RoomAlertMessageHandler, false);

            MsfTimer.WaitForEndOfFrame(() => {
                // if we need to set new online scene
                if (Msf.Options.Has(MsfDictKeys.onlineSceneName))
                {
                    RoomManager.onlineScene = Msf.Options.AsString(MsfDictKeys.onlineSceneName);
                }

                // if we need to set new offline scene
                if (Msf.Options.Has(MsfDictKeys.offlineSceneName))
                {
                    RoomManager.offlineScene = Msf.Options.AsString(MsfDictKeys.offlineSceneName);
                }

                // Disable autoStartInEditor if autoStartRoomClient client option is set
                if (Msf.Options.Has(MsfDictKeys.autoStartRoomClient))
                {
                    autoStartInEditor = false;
                }

                if (Msf.Runtime.IsEditor && autoStartInEditor)
                {
                    MsfTimer.WaitForSeconds(1f, () =>
                    {
                        StartRoomClient(true);
                    });
                }

                if ((Msf.Options.Has(MsfDictKeys.autoStartRoomClient) || Msf.Args.StartClientConnection))
                {
                    StartRoomClient();
                }
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            NetworkClient.UnregisterHandler<RoomAccessValidationResultMessage>();
            NetworkClient.UnregisterHandler<RoomAlertMessage>();
        }

        protected virtual void OnApplicationQuit()
        {
            if (Connection != null)
                Connection.Disconnect();
        }

        /// <summary>
        /// Fire when room server sends alert message to client
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void RoomAlertMessageHandler(NetworkConnection conn, RoomAlertMessage msg)
        {
            switch (msg.Code)
            {
                case RoomAlertMessageCode.Warning:
                    logger.Warn(msg.Message);
                    break;
                case RoomAlertMessageCode.Error:
                    logger.Error(msg.Message);
                    break;
                default:
                    logger.Debug(msg.Message);
                    break;
            }

            OnRoomAlertMessageEvent?.Invoke(msg);
        }

        /// <summary>
        /// Fires when room server send message about access validation result
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void RoomAccessValidationResultMessageHandler(NetworkConnection conn, RoomAccessValidationResultMessage msg)
        {
            if (msg.Status != ResponseStatus.Success)
            {
                logger.Error(msg.Error);

                OnAccessDenied(conn);
                OnAccessDiniedEvent?.Invoke();

                return;
            }

            logger.Debug("Access to server room is validated successfully");

            OnAccessGranted(conn);
            OnAccessGrantedEvent?.Invoke();
        }

        /// <summary>
        /// Test sign in callback
        /// </summary>
        /// <param name="accountInfo"></param>
        /// <param name="error"></param>
        private void SignInCallback(AccountInfoPacket accountInfo, string error)
        {
            if (accountInfo == null)
            {
                logger.Error(error);
                return;
            }

            logger.Debug($"Signed in successfully as {accountInfo.Username}");
            logger.Debug("Finding games...");

            Msf.Client.Matchmaker.FindGames((games) =>
            {
                if (games.Count == 0)
                {
                    logger.Error("No test game found");
                    return;
                }

                logger.Debug($"Found {games.Count} games");

                // Get first game fromlist
                GameInfoPacket firstGame = games.First();

                // Let's try to get access data for room we want to connect to
                GetRoomAccess(firstGame.Id);
            });
        }

        /// <summary>
        /// Tries to get access data for room we want to connect to
        /// </summary>
        /// <param name="roomId"></param>
        private void GetRoomAccess(int roomId)
        {
            logger.Debug($"Getting access to room {roomId}");

            Msf.Client.Rooms.GetAccess(roomId, (access, error) =>
            {
                if (access == null)
                {
                    logger.Error(error);
                    return;
                }

                logger.Debug($"Access to room {roomId} received");
                logger.Debug(access);

                // Save gotten room access
                roomAccess = access;

                // Let's set the IP before we start connection
                RoomManager.SetAddress(roomAccess.RoomIp);

                // Let's set the port before we start connection
                RoomManager.SetPort((ushort)roomAccess.RoomPort);

                logger.Debug("Connecting to room server...");

                MsfTimer.WaitWhile(() => !NetworkClient.isConnected, isSuccessful =>
                {
                    if (!isSuccessful)
                    {
                        RoomManager.StopClient();
                        logger.Error("Connection attempts to room server timed out");
                        return;
                    }
                }, 10);

                // Start mirror client
                RoomManager.StartClient();
            });
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
                    logger.Error("Failed to connect room client to master server");
                }
                else
                {
                    logger.Info($"Successfully connected to {Connection.ConnectionIp}:{Connection.ConnectionPort}");

                    // For the test purpose only
                    if (Msf.Runtime.IsEditor && autoStartInEditor)
                    {
                        if (signInAsGuest)
                        {
                            // Sign in client as guest
                            Msf.Client.Auth.SignInAsGuest(SignInCallback);
                        }
                        else
                        {
                            // Sign in client using credentials
                            Msf.Client.Auth.SignIn(username, password, SignInCallback);
                        }
                    }
                    else
                    {
                        // If we have option with room id
                        // this approach can be used when you have come to this scene from another one.
                        // Set this option before this room client controller is connected to master server
                        if (Msf.Options.Has(MsfDictKeys.roomId))
                        {
                            // Let's try to get access data for room we want to connect to
                            GetRoomAccess(Msf.Options.AsInt(MsfDictKeys.roomId));
                        }
                        else
                        {
                            logger.Error($"You have no room id in this options: {Msf.Options}");
                        }
                    }
                }
            }, 4f);
        }

        /// <summary>
        /// Fires when access to room server granted
        /// </summary>
        /// <param name="conn"></param>
        protected virtual void OnAccessGranted(NetworkConnection conn)
        {
            if (RoomManager.autoCreatePlayer)
            {
                CreatePlayer();
            }
        }

        /// <summary>
        /// Fires when access to room server denied
        /// </summary>
        /// <param name="conn"></param>
        protected virtual void OnAccessDenied(NetworkConnection conn)
        {

        }

        /// <summary>
        /// Fired when mirror client is successfully connected to mirror server
        /// </summary>
        /// <param name="conn"></param>
        public void OnMirrorClientConnected(NetworkConnection conn)
        {
            logger.Debug("Connected to room server successfully");
            logger.Debug($"Sending validation token [{roomAccess.Token}] to room server ");

            // Send validation message to room server
            conn.Send(new ValidateRoomAccessRequestMessage(roomAccess.Token));
        }

        /// <summary>
        /// Start room client
        /// </summary>
        /// <param name="ignoreForceClientMode"></param>
        public void StartRoomClient(bool ignoreForceClientMode = false)
        {
            if (!Msf.Client.Rooms.ForceClientMode && !ignoreForceClientMode) return;

            logger.Info($"Starting Room Client... {Msf.Version}. Multithreading is: {(Msf.Runtime.SupportsThreads ? "On" : "Off")}");
            logger.Info($"Start parameters are: {Msf.Args}");

            // Start connecting room server to master server
            ConnectToMaster();
        }

        /// <summary>
        /// Creates network player
        /// </summary>
        public virtual void CreatePlayer()
        {
            if (!ClientScene.ready) ClientScene.Ready(NetworkClient.connection);
            ClientScene.AddPlayer(NetworkClient.connection);
        }
    }
}