using Barebones.Networking;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class GetPeerAccountInfoResultMessage : IMessageBase
    {
        public string Error { get; set; }
        public ResponseStatus Status { get; set; }

        public void Deserialize(NetworkReader reader)
        {
            Error = reader.ReadString();
            Status = (ResponseStatus)reader.ReadUInt16();
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteString(Error);
            writer.WriteUInt16((ushort)Status);
        }
    }
}