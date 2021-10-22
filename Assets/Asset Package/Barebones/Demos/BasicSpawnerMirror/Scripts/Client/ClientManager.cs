using Aevien.UI;
using Barebones.Bridges.Mirror;
using Barebones.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class ClientManager : MsfBaseClientModule
    {
        private MainView mainView;
        private CreateNewRoomView createNewRoomView;
        private GamesListView gamesListView;

        protected override void OnBeforeClientConnectedToServer()
        {
            DestroyUnwanted();

            // Set cliet mode
            Msf.Client.Rooms.ForceClientMode = true;

            // Set MSF global options
            Msf.Options.Set(MsfDictKeys.autoStartRoomClient, true);
            Msf.Options.Set(MsfDictKeys.offlineSceneName, SceneManager.GetActiveScene().name);

            mainView = ViewsManager.GetView<MainView>("MainView");
            createNewRoomView = ViewsManager.GetView<CreateNewRoomView>("CreateNewRoomView");
            gamesListView = ViewsManager.GetView<GamesListView>("GamesListView");

            if (!Msf.Client.Auth.IsSignedIn)
                Msf.Events.Invoke(EventKeys.showLoadingInfo, "Signing in... Please wait!");
            else
                Msf.Events.Invoke(EventKeys.hideLoadingInfo);

            if (Connection.IsConnected)
            {
                OnClientConnectedToServer();
            }
            else
            {
                FindObjectOfType<ClientToMasterConnector>()?.StartConnection();
            }
        }

        private void DestroyUnwanted()
        {
            FindObjectOfType<MirrorRoomManager>()?.Destroy();
        }

        protected override void OnClientConnectedToServer()
        {
            MsfTimer.WaitForSeconds(1f, () =>
            {
                if (!Msf.Client.Auth.IsSignedIn)
                {
                    Msf.Client.Auth.SignInAsGuest(OnSignedInAsGuest);
                }
                else
                {
                    mainView?.Show();
                }
            });

        }

        private void OnSignedInAsGuest(AccountInfoPacket accountInfo, string error)
        {
            Msf.Events.Invoke(EventKeys.hideLoadingInfo);

            if (accountInfo == null)
            {
                Msf.Events.Invoke(EventKeys.showOkDialogBox, new OkDialogBoxViewEventMessage(error));

                logger.Error(error);
                return;
            }

            mainView?.Show();

            logger.Info("Successfully signed in!");
        }

        public void CreateNewRoom()
        {
            createNewRoomView.Hide();

            Msf.Events.Invoke(EventKeys.showLoadingInfo, "Starting room... Please wait!");

            // Spawn options for spawner controller
            var spawnOptions = new DictionaryOptions();
            spawnOptions.Add(MsfDictKeys.maxPlayers, createNewRoomView.MaxConnections);
            spawnOptions.Add(MsfDictKeys.roomName, createNewRoomView.RoomName);

            // Custom options that will be given to room directly
            var customSpawnOptions = new DictionaryOptions();
            customSpawnOptions.Add(Msf.Args.Names.StartClientConnection, string.Empty);

            Msf.Client.Spawners.RequestSpawn(spawnOptions, customSpawnOptions, createNewRoomView.RegionName, (controller, error) =>
            {
                if (controller == null) {
                    Msf.Events.Invoke(EventKeys.hideLoadingInfo);
                    Msf.Events.Invoke(EventKeys.showOkDialogBox, new OkDialogBoxViewEventMessage(error, null));
                    return;
                }

                Msf.Events.Invoke(EventKeys.showLoadingInfo, "Room started. Finalizing... Please wait!");

                MsfTimer.WaitWhile(() =>
                {
                    return controller.Status != SpawnStatus.Finalized;
                }, (isSuccess) =>
                {
                    Msf.Events.Invoke(EventKeys.hideLoadingInfo);

                    if (!isSuccess)
                    {
                        Msf.Client.Spawners.AbortSpawn(controller.SpawnTaskId);
                        logger.Error("Failed spawn new room. Time is up!");
                        Msf.Events.Invoke(EventKeys.showOkDialogBox, new OkDialogBoxViewEventMessage("Failed spawn new room. Time is up!", null));
                        return;
                    }

                    gamesListView.Show();
                    mainView.Hide();

                    logger.Info("You have successfully spawned new room");
                }, 60f);
            });
        }

        public void Quit()
        {
            Msf.Runtime.Quit();
        }
    }
}
