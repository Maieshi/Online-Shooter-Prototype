using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Barebones.Bridges.Mirror
{
    [AddComponentMenu("Network/MSF/MirrorRoomManager")]
    public class MirrorRoomManager : NetworkManager
    {
        #region INSPECTOR

        [Header("Components"), SerializeField]
        protected MirrorRoomClient mirrorRoomClient;
        [SerializeField]
        protected MirrorRoomServer mirrorRoomServer;

        /// <summary>
        /// Log level of this module
        /// </summary>
        [Header("Room Manager Settings"), SerializeField]
        protected LogLevel logLevel = LogLevel.Info;

        #endregion

        /// <summary>
        /// Logger assigned to this module
        /// </summary>
        protected Logging.Logger logger;

        public override void Awake()
        {
            base.Awake();

            logger = Msf.Create.Logger(GetType().Name);
            logger.LogLevel = logLevel;
        }

        public override void Start()
        {
            // Setup room server
            if (!mirrorRoomServer)
            {
                mirrorRoomServer = FindObjectOfType<MirrorRoomServer>();
                mirrorRoomServer.RoomManager = this;
                mirrorRoomServer.transform.SetParent(transform);
            }

            // Setup room client
            if (!mirrorRoomClient)
            {
                mirrorRoomClient = FindObjectOfType<MirrorRoomClient>();
                mirrorRoomClient.RoomManager = this;
                mirrorRoomClient.transform.SetParent(transform);
            }

            // If room port is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.RoomPort))
            {
                SetPort((ushort)Msf.Args.RoomPort);
            }

            // If the max number of connection is passed through command line
            maxConnections = Msf.Args.ExtractValueInt(Msf.Args.Names.RoomMaxConnections, maxConnections);
        }

        #region MIRROR_CALLBACKS

        /// <summary>
        /// Destroy rooom manager
        /// </summary>
        public void Destroy()
        {
            StopServer();
            StopClient();

            Destroy(gameObject);
        }

        /// <summary>
        /// When mirror server is started
        /// </summary>
        public override void OnStartServer()
        {
            // Say to room server about this
            mirrorRoomServer.OnMirrorServerStarted();
        }

        /// <summary>
        /// When client disconnected from mirror server
        /// </summary>
        /// <param name="conn"></param>
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Notify room server about this
            mirrorRoomServer.OnPlayerLeftRoomHandler(conn);

            // Destroy player
            NetworkServer.DestroyPlayerForConnection(conn);
        }

        /// <summary>
        /// When we connected to mirror server
        /// </summary>
        /// <param name="conn"></param>
        public override void OnClientConnect(NetworkConnection conn)
        {
            mirrorRoomClient.OnMirrorClientConnected(conn);
        }

        #endregion

        /// <summary>
        /// Sets an address 
        /// </summary>
        /// <param name="roomAddress"></param>
        public void SetAddress(string roomAddress)
        {
            networkAddress = roomAddress;
        }

        /// <summary>
        /// Gets an address
        /// </summary>
        /// <param name="roomIp"></param>
        public string GetAddress()
        {
            return networkAddress;
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
