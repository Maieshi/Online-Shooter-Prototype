using Barebones.MasterServer;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.Bridges.Mirror
{
    public class MirrorRoomPlayer
    {
        public MirrorRoomPlayer()
        {
        }

        public MirrorRoomPlayer(int msfPeerId, NetworkConnection mirrorPeer, string username, DictionaryOptions customOptions)
        {
            MsfPeerId = msfPeerId;
            MirrorPeer = mirrorPeer ?? throw new ArgumentNullException(nameof(mirrorPeer));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            CustomOptions = customOptions ?? throw new ArgumentNullException(nameof(customOptions));
        }

        public int MsfPeerId { get; set; }
        public NetworkConnection MirrorPeer { get; set; }
        public string Username { get; set; }
        public DictionaryOptions CustomOptions { get; set; }
    }
}
