using Barebones.Logging;
using Barebones.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Barebones.MasterServer
{
    public abstract class ServerBehaviour : MonoBehaviour, IServer
    {
        /// <summary>
        /// Server socket
        /// </summary>
        private IServerSocket socket;

        /// <summary>
        /// Server messages handlers list
        /// </summary>
        private Dictionary<short, IPacketHandler> handlers;

        /// <summary>
        /// Server modules handlers
        /// </summary>
        private Dictionary<Type, IBaseServerModule> modules;

        /// <summary>
        /// Initialized server modules list
        /// </summary>
        private HashSet<Type> initializedModules;

        /// <summary>
        /// List of connected clients to server
        /// </summary>
        private Dictionary<int, IPeer> connectedPeers;

        /// <summary>
        ///  List of connected clients to server by guids
        /// </summary>
        private Dictionary<Guid, IPeer> peersByGuidLookup;

        /// <summary>
        /// Just message constant
        /// </summary>
        protected const string internalServerErrorMessage = "Internal Server Error";

        /// <summary>
        /// Current server behaviour <see cref="Logger"/>
        /// </summary>
        protected Logging.Logger logger;

        [SerializeField]
        private HelpBox hpInfo = new HelpBox()
        {
            Text = "This component is responsible for starting a Server and initializing its modules",
            Type = HelpBoxType.Info
        };

        [Header("Base Server Settings")]
        [SerializeField, Tooltip("If true, will look for game objects with modules in scene, and initialize them")]
        private bool lookForModules = true;

        [SerializeField, Tooltip("If true, will go through children of this GameObject, and initialize modules that are found on the way")]
        private bool lookInChildrenOnly = true;

        [SerializeField]
        private List<PermissionEntry> permissions;

        [SerializeField, Range(10, 120), Tooltip("Frame rate the server will be running. You can limit it from 10 - 120")]
        private int targetFrameRate = 30;

        [SerializeField, Tooltip("Log level of this script")]
        protected LogLevel logLevel = LogLevel.Info;

        [SerializeField, Tooltip("If true server will try to listen to your public IP address. This feature is for quick way to get IP of the machine on which the server is running. Do not use it on your local machine.")]
        protected bool usePublicIp = false;

        [SerializeField, Tooltip("IP address, to which server will listen to")]
        protected string serverIP = "127.0.0.1";

        [SerializeField, Tooltip("Port, to which server will listen to")]
        protected int serverPort = 5000;

        [Header("Editor Settings"), SerializeField]
        private HelpBox hpEditor = new HelpBox()
        {
            Text = "Editor settings are used only while running in editor",
            Type = HelpBoxType.Warning
        };

        [SerializeField]
        protected bool autoStartInEditor = true;


        /// <summary>
        /// Check if current server is running
        /// </summary>
        public bool IsRunning { get; protected set; } = false;

        /// <summary>
        /// Fires when any client connected to server
        /// </summary>
        public event PeerActionHandler OnPeerConnectedEvent;

        /// <summary>
        /// Fires when any client disconnected from server
        /// </summary>
        public event PeerActionHandler OnPeerDisconnectedEvent;

        /// <summary>
        /// Fires when server started
        /// </summary>
        public event Action OnServerStartedEvent;

        /// <summary>
        /// Fires when server stopped
        /// </summary>
        public event Action OnServerStoppedEvent;

        protected virtual void Awake()
        {
            Application.targetFrameRate = targetFrameRate;

            logger = Msf.Create.Logger(GetType().Name);
            logger.LogLevel = logLevel;

            connectedPeers = new Dictionary<int, IPeer>();
            modules = new Dictionary<Type, IBaseServerModule>();
            initializedModules = new HashSet<Type>();
            handlers = new Dictionary<short, IPacketHandler>();
            peersByGuidLookup = new Dictionary<Guid, IPeer>();

            // Create the server 
            socket = Msf.Create.ServerSocket();

            socket.OnClientConnectedEvent += OnConnectedEventHandle;
            socket.OnClientDisconnectedEvent += OnDisconnectedEventHandler;

            // AesKey handler
            SetHandler((short)MsfMessageCodes.AesKeyRequest, GetAesKeyRequestHandler);
            SetHandler((short)MsfMessageCodes.PermissionLevelRequest, PermissionLevelRequestHandler);
            SetHandler((short)MsfMessageCodes.PeerGuidRequest, GetPeerGuidRequestHandler);
        }

        protected virtual void Start()
        {
            if (Msf.Runtime.IsEditor && autoStartInEditor)
            {
                // Start the server on next frame
                MsfTimer.WaitForEndOfFrame(() =>
                {
                    WaitPublicIpIfRequired(() =>
                    {
                        StartServer();
                    });
                });
            }
        }

        /// <summary>
        /// If <see cref="usePublicIp"/> is set to true this method will wait before we get our IP. Otherwise method will be invoked immidiately
        /// </summary>
        protected virtual void WaitPublicIpIfRequired(Action callback)
        {
            if (usePublicIp)
            {
                logger.Info("Trying to get public IP...");

                Msf.Helper.GetPublicIp((ipInfo) =>
                {
                    logger.Info($"Our public IP is {ipInfo.Ip}");
                    SetIpAddress(ipInfo.Ip);
                    callback?.Invoke();
                });
            }
            else
            {
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Sets the server IP
        /// </summary>
        /// <param name="listenToIp"></param>
        public void SetIpAddress(string listenToIp)
        {
            serverIP = listenToIp;
        }

        /// <summary>
        /// Sets the server port
        /// </summary>
        /// <param name="listenToPort"></param>
        public void SetPort(int listenToPort)
        {
            serverPort = listenToPort;
        }

        /// <summary>
        /// Start server
        /// </summary>
        public virtual void StartServer()
        {
            StartServer(serverIP, serverPort);
        }

        /// <summary>
        /// Start server with given port
        /// </summary>
        /// <param name="listenToPort"></param>
        public virtual void StartServer(int listenToPort)
        {
            StartServer(serverIP, listenToPort);
        }

        /// <summary>
        /// Start server with given port and ip
        /// </summary>
        /// <param name="listenToIp">IP который слшаем</param>
        /// <param name="listenToPort"></param>
        public virtual void StartServer(string listenToIp, int listenToPort)
        {
            socket.Listen(listenToIp, listenToPort);
            LookForModules();
            IsRunning = true;
            OnServerStartedEvent?.Invoke();
            OnStartedServer();
        }

        private void LookForModules()
        {
            if (lookForModules)
            {
                // Find modules
                var modules = lookInChildrenOnly ? GetComponentsInChildren<BaseServerModule>() : FindObjectsOfType<BaseServerModule>();

                // Add modules
                foreach (var module in modules)
                {
                    AddModule(module);
                }

                // Initialize modules
                InitializeModules();

                // Check and notify if some modules are not uninitialized
                var uninitializedModules = GetUninitializedModules();

                if (uninitializedModules.Count > 0)
                {
                    logger.Warn($"Some of the {GetType().Name} modules failed to initialize: \n{string.Join(" \n", uninitializedModules.Select(m => m.GetType().ToString()).ToArray())}");
                }
            }
        }

        /// <summary>
        /// Stp server
        /// </summary>
        public virtual void StopServer()
        {
            IsRunning = false;
            socket.Stop();
            OnServerStoppedEvent?.Invoke();
            OnStoppedServer();
        }

        private void OnConnectedEventHandle(IPeer peer)
        {
            logger.Debug($"Client {peer.Id} connected to server. Total clients are: {connectedPeers.Count + 1}");

            // Listen to messages
            peer.OnMessageReceivedEvent += OnMessageReceived;

            // Save the peer
            connectedPeers[peer.Id] = peer;

            // Create the security extension
            var extension = peer.AddExtension(new SecurityInfoPeerExtension());

            // Set default permission level
            extension.PermissionLevel = 0;

            // Create a unique peer guid
            extension.UniqueGuid = Guid.NewGuid();
            peersByGuidLookup[extension.UniqueGuid] = peer;

            // Invoke the event
            OnPeerConnectedEvent?.Invoke(peer);
            OnPeerConnected(peer);
        }

        private void OnDisconnectedEventHandler(IPeer peer)
        {
            logger.Debug($"Client {peer.Id} disconnected from server. Total clients are: {connectedPeers.Count - 1}");

            // Remove listener to messages
            peer.OnMessageReceivedEvent -= OnMessageReceived;

            // Remove the peer
            connectedPeers.Remove(peer.Id);

            var extension = peer.GetExtension<SecurityInfoPeerExtension>();
            if (extension != null)
            {
                // Remove from guid lookup
                peersByGuidLookup.Remove(extension.UniqueGuid);
            }

            // Invoke the event
            OnPeerDisconnectedEvent?.Invoke(peer);
            OnPeerDisconnected(peer);
        }

        protected virtual void OnDestroy()
        {
            socket.OnClientConnectedEvent -= OnConnectedEventHandle;
            socket.OnClientDisconnectedEvent -= OnDisconnectedEventHandler;
        }

        #region MESSAGE HANDLERS

        protected virtual void GetPeerGuidRequestHandler(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<SecurityInfoPeerExtension>();
            message.Respond(extension.UniqueGuid.ToByteArray(), ResponseStatus.Success);
        }

        protected virtual void PermissionLevelRequestHandler(IIncommingMessage message)
        {
            var key = message.AsString();

            var extension = message.Peer.GetExtension<SecurityInfoPeerExtension>();

            var currentLevel = extension.PermissionLevel;
            var newLevel = currentLevel;

            var permissionClaimed = false;

            foreach (var entry in permissions)
            {
                if (entry.key == key)
                {
                    newLevel = entry.permissionLevel;
                    permissionClaimed = true;
                }
            }

            extension.PermissionLevel = newLevel;

            if (!permissionClaimed && !string.IsNullOrEmpty(key))
            {
                // If we didn't claim a permission
                message.Respond("Invalid permission key", ResponseStatus.Unauthorized);
                return;
            }

            message.Respond(newLevel, ResponseStatus.Success);
        }

        protected virtual void GetAesKeyRequestHandler(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<SecurityInfoPeerExtension>();
            var encryptedKey = extension.AesKeyEncrypted;

            if (encryptedKey != null)
            {
                logger.Debug("There's already a key generated");

                // There's already a key generated
                message.Respond(encryptedKey, ResponseStatus.Success);
                return;
            }

            // Generate a random key
            var aesKey = Msf.Helper.CreateRandomString(8);

            var clientsPublicKeyXml = message.AsString();

            // Deserialize public key
            var sr = new System.IO.StringReader(clientsPublicKeyXml);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            var clientsPublicKey = (RSAParameters)xs.Deserialize(sr);

            using (var csp = new RSACryptoServiceProvider())
            {
                csp.ImportParameters(clientsPublicKey);
                var encryptedAes = csp.Encrypt(Encoding.Unicode.GetBytes(aesKey), false);

                // Save keys for later use
                extension.AesKeyEncrypted = encryptedAes;
                extension.AesKey = aesKey;

                message.Respond(encryptedAes, ResponseStatus.Success);
            }
        }

        #endregion

        #region VIRTUAL METHODS

        /// <summary>
        /// Invokes when new <see cref="IPeer"/> connected
        /// </summary>
        /// <param name="peer"></param>
        protected virtual void OnPeerConnected(IPeer peer) { }

        /// <summary>
        /// Invokes when existing <see cref="IPeer"/> disconnected
        /// </summary>
        /// <param name="peer"></param>
        protected virtual void OnPeerDisconnected(IPeer peer) { }

        /// <summary>
        /// Invokes when message received
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceived(IIncommingMessage message)
        {
            try
            {
                handlers.TryGetValue(message.OpCode, out IPacketHandler handler);

                if (handler == null)
                {
                    logger.Warn(string.Format("Handler for OpCode {0} does not exist", message.OpCode));

                    if (message.IsExpectingResponse)
                    {
                        message.Respond(internalServerErrorMessage, ResponseStatus.NotHandled);
                        return;
                    }
                    return;
                }

                handler.Handle(message);
            }
            catch (Exception e)
            {
                if (Msf.Runtime.IsEditor)
                {
                    throw;
                }

                logger.Error($"Error while handling a message from Client. OpCode: {message.OpCode}, Error: {e}");

                if (!message.IsExpectingResponse)
                {
                    return;
                }

                try
                {
                    message.Respond(internalServerErrorMessage, ResponseStatus.Error);
                }
                catch (Exception exception)
                {
                    Logs.Error(exception);
                }
            }
        }

        /// <summary>
        /// Invokes when server stopped
        /// </summary>
        protected virtual void OnStoppedServer() { }

        /// <summary>
        /// Invokes when server started
        /// </summary>
        protected virtual void OnStartedServer()
        {
            if (lookForModules)
            {
                var initializedModules = GetInitializedModules();

                if (initializedModules.Count > 0)
                {
                    logger.Info($"Successfully initialized modules: \n{string.Join(" \n", initializedModules.Select(m => m.GetType().ToString()).ToArray())}");
                }
                else
                {
                    logger.Info("No modules found");
                }
            }
        }

        #endregion

        #region IServer

        /// <summary>
        /// Add new module to list
        /// </summary>
        /// <param name="module"></param>
        public void AddModule(IBaseServerModule module)
        {
            if (modules.ContainsKey(module.GetType()))
            {
                throw new Exception("A module already exists in the server: " + module.GetType());
            }

            modules[module.GetType()] = module;
        }

        /// <summary>
        /// Add new module to list and start it
        /// </summary>
        /// <param name="module"></param>
        public void AddModuleAndInitialize(IBaseServerModule module)
        {
            AddModule(module);
            InitializeModules();
        }

        /// <summary>
        /// Check is server contains module with given name
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public bool ContainsModule(IBaseServerModule module)
        {
            return modules.ContainsKey(module.GetType());
        }

        /// <summary>
        /// Start all asigned modules
        /// </summary>
        /// <returns></returns>
        public bool InitializeModules()
        {
            var checkOptional = true;

            // Initialize modules
            while (true)
            {
                var changed = false;
                foreach (var entry in modules)
                {
                    // Module is already initialized
                    if (initializedModules.Contains(entry.Key))
                    {
                        continue;
                    }

                    // Not all dependencies have been initialized
                    if (!entry.Value.Dependencies.All(d => initializedModules.Any(d.IsAssignableFrom)))
                    {
                        continue;
                    }

                    // Not all OPTIONAL dependencies have been initialized
                    if (checkOptional && !entry.Value.OptionalDependencies.All(d => initializedModules.Any(d.IsAssignableFrom)))
                    {
                        continue;
                    }

                    // If we got here, we can initialize our module
                    entry.Value.Server = this;
                    entry.Value.Initialize(this);
                    initializedModules.Add(entry.Key);

                    // Keep checking optional if something new was initialized
                    checkOptional = true;

                    changed = true;
                }

                // If we didn't change anything, and initialized all that we could
                // with optional dependencies in mind
                if (!changed && checkOptional)
                {
                    // Initialize everything without checking optional dependencies
                    checkOptional = false;
                    continue;
                }

                // If we can no longer initialize anything
                if (!changed)
                {
                    return !GetUninitializedModules().Any();
                }
            }
        }

        /// <summary>
        /// Get <see cref="IBaseServerModule"/> module
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetModule<T>() where T : class, IBaseServerModule
        {
            modules.TryGetValue(typeof(T), out IBaseServerModule module);

            if (module == null)
            {
                // Try to find an assignable module
                module = modules.Values.FirstOrDefault(m => m is T);
            }

            return module as T;
        }

        /// <summary>
        /// Get all modules that are already started
        /// </summary>
        /// <returns></returns>
        public List<IBaseServerModule> GetInitializedModules()
        {
            return modules
                .Where(m => initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        /// <summary>
        /// Get all modules that are not started yet
        /// </summary>
        /// <returns></returns>
        public List<IBaseServerModule> GetUninitializedModules()
        {
            return modules
                .Where(m => !initializedModules.Contains(m.Key))
                .Select(m => m.Value)
                .ToList();
        }

        /// <summary>
        /// Set message handler
        /// </summary>
        /// <param name="handler"></param>
        public void SetHandler(IPacketHandler handler)
        {
            handlers[handler.OpCode] = handler;
        }

        /// <summary>
        /// Set message handler
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="handler"></param>
        public void SetHandler(short opCode, IncommingMessageHandler handler)
        {
            handlers[opCode] = new PacketHandler(opCode, handler);
        }

        /// <summary>
        /// Get connected <see cref="IPeer"/>
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public IPeer GetPeer(int peerId)
        {
            connectedPeers.TryGetValue(peerId, out IPeer peer);
            return peer;
        }

        #endregion

        [Serializable]
        public class PermissionEntry
        {
            public string key;
            public int permissionLevel;
        }
    }
}