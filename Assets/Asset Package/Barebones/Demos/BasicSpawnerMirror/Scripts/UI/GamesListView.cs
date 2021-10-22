using Aevien.UI;
using Barebones.Logging;
using Barebones.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class GamesListView : UIView
    {
        [Header("Components"), SerializeField]
        private GameItem gameItemPrefab;
        [SerializeField]
        private RectTransform listContainer;

        protected override void OnShow()
        {
            FindGames();
        }

        public void FindGames()
        {
            Msf.Events.Invoke(EventKeys.showLoadingInfo, "Finding rooms... Please wait!");

            MsfTimer.WaitForSeconds(1f, () =>
            {
                Msf.Client.Matchmaker.FindGames((games) =>
                {
                    Msf.Events.Invoke(EventKeys.hideLoadingInfo);

                    if (games.Count == 0)
                    {
                        Msf.Events.Invoke(EventKeys.showOkDialogBox, new OkDialogBoxViewEventMessage("No games found!"));
                        return;
                    }

                    DrawGamesList(games);
                });
            });
        }

        private void DrawGamesList(IEnumerable<GameInfoPacket> games)
        {
            ClearGamesList();

            if (listContainer && gameItemPrefab)
            {
                foreach (GameInfoPacket game in games)
                {
                    var gameItemInstance = Instantiate(gameItemPrefab, listContainer, false);
                    gameItemInstance.SetInfo(game, this);

                    Logs.Info(game);
                }
            }
            else
            {
                Logs.Error("Not all components are setup");
            }
        }

        private void ClearGamesList()
        {
            if (listContainer)
            {
                foreach (Transform tr in listContainer)
                {
                    Destroy(tr.gameObject);
                }
            }
        }

        public void StartGame(GameInfoPacket gameInfo)
        {
            Msf.Options.Set(MsfDictKeys.autoStartRoomClient, true);
            Msf.Options.Set(MsfDictKeys.roomId, gameInfo.Id);

            ScenesLoader.LoadSceneByName("Room", (progressValue) =>
            {
                Msf.Events.Invoke(EventKeys.showLoadingInfo, $"Loading scene {Mathf.RoundToInt(progressValue * 100f)}% ... Please wait!");
            }, null);
        }
    }
}