using Aevien.Utilities;
using Barebones.Logging;
using Barebones.Networking;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Automatically connects to master server
    /// </summary>
    public class ClientToMasterConnector : ConnectionHelper
    {
        protected override void Awake()
        {
            // If master IP is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.MasterIp))
            {
                serverIp = Msf.Args.MasterIp;
            }

            // If master port is provided via cmd arguments
            if (Msf.Args.IsProvided(Msf.Args.Names.MasterPort))
            {
                serverPort = Msf.Args.MasterPort;
            }

            Connection = Msf.Client.Connection;

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            if (Msf.Args.StartClientConnection)
            {
                StartConnection();
            }
        }
    }
}