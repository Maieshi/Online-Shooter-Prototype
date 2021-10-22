using Aevien.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class GameItem : MonoBehaviour
    {
        public TextMeshProUGUI gameNameText;
        public TextMeshProUGUI gameAddressText;
        public TextMeshProUGUI gameRegionText;
        public TextMeshProUGUI gamePlayersText;

        public UIButton connectButton;

        public void SetInfo(GameInfoPacket gameInfo, GamesListView owner)
        {
            if (gameNameText)
            {
                gameNameText.text = gameInfo.Name;
            }

            if (gameAddressText)
            {
                gameAddressText.text = gameInfo.Address;
            }

            if (gameRegionText)
            {
                string region = string.IsNullOrEmpty(gameInfo.Region) ? "International" : gameInfo.Region;
                gameRegionText.text = $"Region: <color=yellow>{region}</color>";
            }

            if (gamePlayersText)
            {
                string maxPleyers = gameInfo.MaxPlayers <= 0 ? "∞" : gameInfo.MaxPlayers.ToString();
                gamePlayersText.text = $"Players: <color=yellow>{gameInfo.OnlinePlayers} / {maxPleyers}</color>";
            }

            if (connectButton)
            {
                connectButton.AddOnClickListener(() => {
                    owner.StartGame(gameInfo);
                });
            }
        }
    }
}