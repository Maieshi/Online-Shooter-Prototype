using Aevien.Utilities;
using Barebones.Logging;
using Barebones.Networking;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;


namespace Barebones.MasterServer
{
    public abstract class ConnectionHelper : Singleton<ConnectionHelper>
    {
        protected int currentAttemptToConnect = 0;
        protected Logging.Logger logger;

        [SerializeField]
        protected HelpBox header = new HelpBox()
        {
            Text = "This script connects client to server. Is is just a helper",
            Type = HelpBoxType.Info
        };

        [Tooltip("Log level of this script"), SerializeField]
        protected LogLevel logLevel = LogLevel.Info;

        [Tooltip("Address to the server"), SerializeField]
        protected string serverIp = "127.0.0.1";

        [Tooltip("Port of the server"), SerializeField]
        protected int serverPort = 5000;

        [Header("Advanced"), SerializeField]
        protected float minTimeToConnect = 0.5f;
        [SerializeField]
        protected float maxTimeToConnect = 4f;
        [SerializeField]
        protected float timeToConnect = 0.5f;
        [SerializeField]
        protected int maxAttemptsToConnect = 5;

        [Header("Editor Settings"), SerializeField]
        private HelpBox roomServerInfo = new HelpBox()
        {
            Text = "Editor settings works only in editor",
            Type = HelpBoxType.Info
        };

        [Tooltip("If true, will try to connect on the Start()"), SerializeField]
        protected bool autoStartInEditor = false;

        [Header("Events")]
        /// <summary>
        /// Triggers when connected to server
        /// </summary>
        public UnityEvent OnConnectedEvent;

        /// <summary>
        /// triggers when disconnected from server
        /// </summary>
        public UnityEvent OnDisconnectedEvent;

        /// <summary>
        /// Main connection to server
        /// </summary>
        public IClientSocket Connection { get; protected set; }

        protected override void Awake()
        {
            base.Awake();

            logger = Msf.Create.Logger(typeof(ClientToMasterConnector).Name);
            logger.LogLevel = logLevel;

            // In case this object is not at the root level of hierarchy
            // move it there, so that it won't be destroyed
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }
        }

        protected virtual void OnValidate()
        {
            if (maxAttemptsToConnect <= 0) maxAttemptsToConnect = 1;
        }

        protected virtual void Start()
        {
            if (Msf.Runtime.IsEditor && autoStartInEditor)
            {
                StartConnection();
            }
        }

        /// <summary>
        /// Sets the master server IP
        /// </summary>
        /// <param name="masterIp"></param>
        public void SetIpAddress(string masterIp)
        {
            this.serverIp = masterIp;
        }

        /// <summary>
        /// Sets the master server port
        /// </summary>
        /// <param name="masterPort"></param>
        public void SetPort(int masterPort)
        {
            this.serverPort = masterPort;
        }

        /// <summary>
        /// Starts connection to master server
        /// </summary>
        public void StartConnection()
        {
            StartCoroutine(StartConnectionProcess(serverIp, serverPort, maxAttemptsToConnect));
        }

        public void StartConnection(int numberOfAttempts)
        {
            StartCoroutine(StartConnectionProcess(serverIp, serverPort, numberOfAttempts));
        }

        public void StartConnection(string serverIp, int serverPort, int numberOfAttempts = 5)
        {
            StartCoroutine(StartConnectionProcess(serverIp, serverPort, numberOfAttempts));
        }

        protected virtual IEnumerator StartConnectionProcess(string serverIp, int serverPort, int numberOfAttempts)
        {
            currentAttemptToConnect = 0;
            maxAttemptsToConnect = numberOfAttempts;

            // Wait a fraction of a second, in case we're also starting a master server at the same time
            yield return new WaitForSeconds(0.2f);

            if (!Connection.IsConnected)
                logger.Info($"Starting MSF Client... {Msf.Version}. Multithreading is: {(Msf.Runtime.SupportsThreads ? "On" : "Off")}");

            Connection.AddConnectionListener(OnConnectedEventHandler);

            while (true)
            {
                // If is already connected break cycle
                if (Connection.IsConnected)
                {
                    yield break;
                }

                // If currentAttemptToConnect of attemts is equals maxAttemptsToConnect stop connection
                if (currentAttemptToConnect == maxAttemptsToConnect)
                {
                    logger.Info($"Client cannot to connect to MSF server at: {serverIp}:{serverPort}");
                    Connection.Disconnect();
                    yield break;
                }

                // If we got here, we're not connected
                if (Connection.IsConnecting)
                {
                    if (maxAttemptsToConnect > 0)
                    {
                        currentAttemptToConnect++;
                    }

                    logger.Info($"Retrying to connect to MSF server at: {serverIp}:{serverPort}");
                }
                else
                {
                    logger.Info($"Connecting to MSF server at: {serverIp}:{serverPort}");
                }

                if (!Connection.IsConnected)
                {
                    Connection.Connect(serverIp, serverPort);
                }

                // Give a few seconds to try and connect
                yield return new WaitForSeconds(timeToConnect);

                // If we're still not connected
                if (!Connection.IsConnected)
                {
                    timeToConnect = Mathf.Min(timeToConnect * 2, maxTimeToConnect);
                }
            }
        }

        protected virtual void OnDisconnectedEventHandler()
        {
            logger.Info($"Disconnected from MSF server");

            timeToConnect = minTimeToConnect;

            Connection.RemoveDisconnectionListener(OnDisconnectedEventHandler);

            OnDisconnectedEvent?.Invoke();
        }

        protected virtual void OnConnectedEventHandler()
        {
            logger.Info($"Connected to MSF server at: {serverIp}:{serverPort}");

            timeToConnect = minTimeToConnect;

            Connection.RemoveConnectionListener(OnConnectedEventHandler);
            Connection.AddDisconnectionListener(OnDisconnectedEventHandler);

            OnConnectedEvent?.Invoke();
        }

        protected virtual void OnApplicationQuit()
        {
            if (Connection != null)
            {
                Connection.Disconnect();
            }
        }
    }
}
