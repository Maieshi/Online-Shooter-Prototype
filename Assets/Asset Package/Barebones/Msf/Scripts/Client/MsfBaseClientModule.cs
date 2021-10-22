using Barebones.Logging;
using Barebones.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer
{
    public abstract class MsfBaseClientModule : MonoBehaviour
    {
        /// <summary>
        /// Client handlers list. Requires for connection changing process. <seealso cref="ChangeConnection(IClientSocket)"/>
        /// </summary>
        private Dictionary<short, IPacketHandler> handlers;

        /// <summary>
        /// Logger assigned to this module
        /// </summary>
        protected Logging.Logger logger;

        /// <summary>
        /// Log levelof this module
        /// </summary>
        [Header("Base Module Settings"), SerializeField]
        protected LogLevel logLevel = LogLevel.Info;

        /// <summary>
        /// Current module connection
        /// </summary>
        public IClientSocket Connection { get; protected set; }

        /// <summary>
        /// Check if current module is connected to server
        /// </summary>
        public bool IsConnected => Connection != null && Connection.IsConnected;

        protected virtual void Awake()
        {
            handlers = new Dictionary<short, IPacketHandler>();

            logger = Msf.Create.Logger(GetType().Name);
            logger.LogLevel = logLevel;
        }

        protected virtual void Start()
        {
            ChangeConnection(ConnectionFactory());
            OnBeforeClientConnectedToServer();
        }

        protected virtual void OnDestroy()
        {
            ClearConnection();
        }

        /// <summary>
        /// Returns the connection to server
        /// </summary>
        /// <returns></returns>
        protected virtual IClientSocket ConnectionFactory()
        {
            return Msf.Client.Connection;
        }

        /// <summary>
        /// Clears connection and all its handlers if <paramref name="clearHandlers"/> is true
        /// </summary>
        public void ClearConnection(bool clearHandlers = true)
        {
            if (Connection != null)
            {
                if (handlers != null && clearHandlers)
                {
                    foreach (var handler in handlers.Values)
                    {
                        Connection.RemoveHandler(handler);
                    }

                    handlers.Clear();
                }

                Connection.OnStatusChangedEvent -= OnConnectionStatusChanged;
                Connection.RemoveConnectionListener(OnClientConnectedToServer);
                Connection.RemoveDisconnectionListener(OnClientDisconnectedFromServer);
            }
        }

        /// <summary>
        /// Sets a message handler to connection, which is used by this this object
        /// to communicate with server
        /// </summary>
        /// <param name="handler"></param>
        public void SetHandler(IPacketHandler handler)
        {
            handlers[handler.OpCode] = handler;
            Connection?.SetHandler(handler);
        }

        /// <summary>
        /// Sets a message handler to connection, which is used by this this object
        /// to communicate with server 
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="handler"></param>
        public void SetHandler(short opCode, IncommingMessageHandler handler)
        {
            SetHandler(new PacketHandler(opCode, handler));
        }

        /// <summary>
        /// Removes the packet handler, but only if this exact handler
        /// was used
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveHandler(IPacketHandler handler)
        {
            Connection?.RemoveHandler(handler);
        }

        /// <summary>
        /// Changes the connection object, and sets all of the message handlers of this object
        /// to new connection.
        /// </summary>
        /// <param name="socket"></param>
        public void ChangeConnection(IClientSocket socket)
        {
            // Clear just connection but not handlers
            ClearConnection(false);

            // Change connections
            Connection = socket;

            // Override packet handlers
            foreach (var packetHandler in handlers.Values)
            {
                socket.SetHandler(packetHandler);
            }

            Connection.OnStatusChangedEvent += OnConnectionStatusChanged;
            OnConnectionSocketChanged(Connection);
            Connection.AddConnectionListener(OnClientConnectedToServer, false);
            Connection.AddDisconnectionListener(OnClientDisconnectedFromServer, false);
        }

        /// <summary>
        /// Fires when this module is started before connection is established
        /// </summary>
        protected virtual void OnBeforeClientConnectedToServer() { }

        /// <summary>
        /// Fires when this module successfully connected to server
        /// </summary>
        protected virtual void OnClientConnectedToServer() { }

        /// <summary>
        /// Fires when this module disconnected from server
        /// </summary>
        protected virtual void OnClientDisconnectedFromServer() { }

        /// <summary>
        /// Fires when connection of this module is changing
        /// </summary>
        /// <param name="socket"></param>
        protected virtual void OnConnectionSocketChanged(IClientSocket socket) { }

        /// <summary>
        /// Fires each time the connection status is changing
        /// </summary>
        /// <param name="status"></param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatus status) { }
    }
}
