using Barebones.Logging;
using Barebones.MasterServer;
using Barebones.Networking;
using CommandTerminal;
using System;
using System.Collections.Generic;
using UnityEngine;
using MsfLogger = Barebones.Logging.Logger;

namespace Barebones.Client.Utilities
{
    public class ClientMatchmakerTerminalCommands : MsfBaseClientModule
    {
        protected override void OnBeforeClientConnectedToServer()
        {
            Terminal.Shell.AddCommand("match.games", GetMatchGamesListCmd, 0, 0, "Get list of game from matchmaking module");
        }

        private void GetMatchGamesListCmd(CommandArg[] args)
        {
            Msf.Client.Matchmaker.FindGames((games) =>
            {
                if (games.Count > 0)
                {
                    foreach(var game in games)
                    {
                        logger.Info(game);
                    }
                }
                else
                {
                    logger.Info("No games found");
                }
            });
        }

        //[RegisterCommand(Name = "client.spawner.start", Help = "Send request to start room. 1 Room Name, 2 Max Connections", MinArgCount = 1)]
        //private static void SendRequestSpawn(CommandArg[] args)
        //{
        //    var options = new DictionaryOptions();
        //    options.Add(MsfDictKeys.roomName, args[0].String.Replace('+', ' '));

        //    if(args.Length > 1)
        //    {
        //        options.Add(MsfDictKeys.maxPlayers, args[1].String);
        //    }

        //    var customOptions = new DictionaryOptions();
        //    customOptions.Add("-myName", "\"John Adams\"");
        //    customOptions.Add("-myAge", 45);
        //    customOptions.Add("-msfStartClientConnection", string.Empty);

        //    Msf.Client.Spawners.RequestSpawn(options, customOptions, string.Empty, (controller, error) => {
        //        if (controller == null) return;

        //        MsfTimer.WaitWhile(() => {
        //            return controller.Status != SpawnStatus.Finalized;
        //        }, (isSuccess) => {

        //            if (!isSuccess)
        //            {
        //                Msf.Client.Spawners.AbortSpawn(controller.SpawnTaskId);
        //                Logs.Error("You have failed to spawn new room");
        //            }

        //            Logs.Info("You have successfully spawned new room");
        //        }, 60f);
        //    });
        //}

        //[RegisterCommand(Name = "client.spawner.abort", Help = "Send request to start room. 1 Process Id", MinArgCount = 1, MaxArgCount = 1)]
        //private static void SendAbortSpawn(CommandArg[] args)
        //{
        //    Msf.Client.Spawners.AbortSpawn(args[0].Int);
        //}
    }
}
