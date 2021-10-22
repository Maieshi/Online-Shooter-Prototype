using Barebones.Networking;
using CommandTerminal;
using Mirror;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class RoomClientBehaviour : MsfBaseClientModule
    {
        /// <summary>
        /// Room access that client gets from master server
        /// </summary>
        protected RoomAccessPacket roomAccess;

        /// <summary>
        /// Just an info about current connection process
        /// </summary>
        private bool isConnecting = false;

        [Header("Master Connection Settings")]
        [SerializeField]
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

        [SerializeField]
        protected float waitInSecBeforeStart = 2f;

        [SerializeField]
        protected bool signInAsGuest = true;

        [SerializeField]
        protected string username = "qwerty";

        [SerializeField]
        protected string password = "qwerty12345";

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

        protected virtual void OnApplicationQuit()
        {
            if (Connection != null)
                Connection.Disconnect();
        }

        protected virtual void OnValidate()
        {
            if (waitInSecBeforeStart < 0f)
            {
                waitInSecBeforeStart = 0f;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            NetworkClient.UnregisterHandler<RoomAccessValidationResultMessage>();
            NetworkClient.UnregisterHandler<RoomAlertMessage>();
            NetworkClient.UnregisterHandler<GetPeerAccountInfoResultMessage>();
        }

        protected override void OnBeforeClientConnectedToServer()
        {
            // In case we are nested to another object
            transform.SetParent(null);

            NetworkClient.RegisterHandler<RoomAccessValidationResultMessage>(RoomAccessValidationResultMessageHandler, false);
            NetworkClient.RegisterHandler<RoomAlertMessage>(RoomAlertMessageHandler, false);
            NetworkClient.RegisterHandler<GetPeerAccountInfoResultMessage>(GetPeerAccountInfoResultMessageHandler, false);

            // if we need to set new online scene
            if (Msf.Options.Has(MsfDictKeys.onlineSceneName))
            {
                NetworkManager.singleton.onlineScene = Msf.Options.AsString(MsfDictKeys.onlineSceneName);
            }

            // if we need to set new offline scene
            if (Msf.Options.Has(MsfDictKeys.offlineSceneName))
            {
                NetworkManager.singleton.offlineScene = Msf.Options.AsString(MsfDictKeys.offlineSceneName);
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
                // Wait a moment in case room server is in the same scene and currently starting
                MsfTimer.WaitForSeconds(waitInSecBeforeStart, () => StartRoomClient(true));
            }

            // Start room client at start if we use StartClientConnection cmd
            if (Msf.Args.StartClientConnection)
            {
                StartRoomClient();
            }
        }

        /// <summary>
        /// Fires when room server sends message to client about its account info that room server has gotten from master server
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void GetPeerAccountInfoResultMessageHandler(NetworkConnection conn, GetPeerAccountInfoResultMessage msg)
        {
            if (msg.Status != ResponseStatus.Success)
            {
                logger.Error(msg.Error);
                return;
            }

            logger.Debug("You have joined the room as player");

            OnJoinedRoomEvent?.Invoke();
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

                OnAccessDiniedEvent?.Invoke();

                return;
            }

            logger.Debug("Access to server room is validated successfully");

            OnAccessGrantedEvent?.Invoke();
        }

        /// <summary>
        /// Fires when this room client manager connected to master as client
        /// </summary>
        protected override void OnClientConnectedToServer()
        {
            isConnecting = false;

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
        /// Fires when this room client manager disconnected from master as client
        /// </summary>
        protected override void OnClientDisconnectedFromServer()
        {
            NetworkClient.Disconnect();
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
                SetIpAddress(roomAccess.RoomIp);

                // Let's set the port before we start connection
                SetPort((ushort)roomAccess.RoomPort);

                logger.Debug("Connecting to room server...");

                MsfTimer.WaitWhile(() => !NetworkClient.isConnected, isSuccessful =>
                {
                    if (!isSuccessful)
                    {
                        logger.Error("Connection attempts to room server timed out");
                        return;
                    }

                    logger.Debug("Connected to room server successfully");
                    logger.Debug($"Sending validation token [{roomAccess.Token}] to room server ");

                    // Send validation message to room server
                    NetworkClient.Send(new ValidateRoomAccessRequestMessage(roomAccess.Token));
                }, 10);

                // Start mirror client
                NetworkManager.singleton.StartClient();
            });
        }

        /// <summary>
        /// Starting connection to master server as client to be able to register room later after successful connection
        /// </summary>
        private void StartConnectionToMaster()
        {
            // If connection process is started
            if (!isConnecting)
                logger.Info($"Connecting room client to master server at: {masterIp}:{masterPort}");
            else
                logger.Info($"Retrying connection of room client to master server at: {masterIp}:{masterPort}");

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
                    logger.Info("Room client is successfuly connected to master server");
                    logger.Info("Starting Mirror client");
                }
            }, 2f);
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

            MsfTimer.WaitForSeconds(1f, () => {
                if (!Connection.IsConnected)
                {
                    // Start the connection to master
                    StartConnectionToMaster();
                }
            });
        }

        /// <summary>
        /// Sets a room server ip address
        /// </summary>
        /// <param name="roomIp"></param>
        public void SetIpAddress(string roomIp)
        {
            NetworkManager.singleton.networkAddress = roomIp;
        }

        /// <summary>
        /// Gets a room server ip address
        /// </summary>
        /// <param name="roomIp"></param>
        public string GetIpAddress()
        {
            return NetworkManager.singleton.networkAddress;
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
    }
}