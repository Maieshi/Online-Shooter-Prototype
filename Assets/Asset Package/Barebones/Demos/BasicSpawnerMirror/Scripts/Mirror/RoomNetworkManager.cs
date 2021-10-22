using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class RoomNetworkManager : NetworkManager
    {
        [Header("Room Components"), SerializeField]
        protected RoomServerBehaviour roomServerBehaviour;
        [SerializeField]
        protected RoomClientBehaviour roomClientBehaviour;

        /// <summary>
        /// Fires when server started
        /// </summary>
        public override void OnStartServer()
        {
            if (!roomServerBehaviour)
                roomServerBehaviour = FindObjectOfType<RoomServerBehaviour>();

            // Start listening to new player is joined
            roomServerBehaviour.OnPlayerJoinedRoomEvent += RoomServerBehaviour_OnPlayerJoinedRoomEvent;
        }

        /// <summary>
        /// Fires when new player joined
        /// </summary>
        /// <param name="roomPlayer"></param>
        private void RoomServerBehaviour_OnPlayerJoinedRoomEvent(RoomPlayer roomPlayer)
        {
            // Here ...
            // We can create player object
            // We can load player profile
        }

        /// <summary>
        /// Fires when server stopped
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();

            if (roomServerBehaviour)
                roomServerBehaviour.OnPlayerJoinedRoomEvent -= RoomServerBehaviour_OnPlayerJoinedRoomEvent;
        }

        /// <summary>
        /// Fires when client started
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!roomClientBehaviour)
                roomClientBehaviour = FindObjectOfType<RoomClientBehaviour>();
        }

        /// <summary>
        /// When client disconnected from mirror server
        /// </summary>
        /// <param name="conn"></param>
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);

            // Notify room server about this
            if (roomServerBehaviour)
                roomServerBehaviour.OnPlayerLeftRoomHandler(conn);
        }
    }
}