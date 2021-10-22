using Barebones.MasterServer;
using CommandTerminal;
using UnityEngine;

namespace Barebones.Client.Utilities
{
    public class ClientConnectionTerminalCommands : MsfBaseClientModule
    {
        protected override void OnBeforeClientConnectedToServer()
        {
            Terminal.Shell.AddCommand("client.ping", SendPingCmd, 0, 0, "Send ping to master server");
            Terminal.Shell.AddCommand("client.connect", ClientConnect, 2, 2, "Connects the client to master. 1 Server IP address, 2 Server port");
            Terminal.Shell.AddCommand("client.disconnect", ClientDisconnect, 0, 0, "Disconnects the client from master");
        }

        private void SendPingCmd(CommandArg[] args)
        {
            Connection.SendMessage((short)MsfMessageCodes.Ping, (status, response) =>
            {
                Debug.Log($"Message: {response.AsString()}, Status: {response.Status.ToString()}");
            });
        }

        private void ClientConnect(CommandArg[] args)
        {
            Connection.Connect(args[0].String, Mathf.Clamp(args[1].Int, 0, ushort.MaxValue));
        }

        private void ClientDisconnect(CommandArg[] args)
        {
            Connection.Disconnect();
        }
    }
}