using Barebones.MasterServer;
using Barebones.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    public class ClientConnectionStatusComponent : MonoBehaviour
    {
        [Header("Settings"), SerializeField]
        private bool changeStatusColor = true;

        [Header("Status Colors"), SerializeField]
        private Color unknownStatusColor = Color.yellow;
        [SerializeField]
        private Color onlineStatusColor = Color.green;
        [SerializeField]
        private Color connectingStatusColor = Color.cyan;
        [SerializeField]
        private Color offlineStatusColor = Color.red;

        [Header("Components"), SerializeField]
        private Image statusImage;
        [SerializeField]
        private TextMeshProUGUI statusText;

        public IClientSocket Connection => Msf.Connection;

        protected virtual void Start()
        {
            Connection.OnStatusChangedEvent += OnStatusChangedEventHandler;
            OnStatusChangedEventHandler(Connection.Status);
        }

        protected virtual void OnStatusChangedEventHandler(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.Connected:
                    RepaintStatus("Client is connected", onlineStatusColor);
                    break;
                case ConnectionStatus.Disconnected:
                    RepaintStatus("Client is offline", offlineStatusColor);
                    break;
                case ConnectionStatus.Connecting:
                    RepaintStatus("Client is connecting", connectingStatusColor);
                    break;
                default:
                    RepaintStatus("Unknown status", unknownStatusColor);
                    break;
            }
        }

        private void RepaintStatus(string statusMsg, Color statusColor)
        {
            if (changeStatusColor && statusImage != null)
                statusImage.color = statusColor;

            if (changeStatusColor && statusText != null)
                statusText.color = statusColor;

            if (statusText != null)
                statusText.text = statusMsg;
        }

        protected virtual void OnDestroy()
        {
            if (Connection != null)
                Connection.OnStatusChangedEvent -= OnStatusChangedEventHandler;
        }
    }
}