using Aevien.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicSpawnerMirror
{
    public class CreateNewRoomView : UIView
    {
        private TMP_InputField roomNameInputField;
        private TMP_InputField roomMaxConnectionsInputField;
        private TMP_InputField regionNameInputField;

        protected override void Start()
        {
            base.Start();

            roomNameInputField = ChildComponent<TMP_InputField>("roomNameInputField");
            roomMaxConnectionsInputField = ChildComponent<TMP_InputField>("roomMaxConnectionsInputField");
            regionNameInputField = ChildComponent<TMP_InputField>("regionNameInputField");
        }

        public string RoomName
        {
            get
            {
                return roomNameInputField != null ? roomNameInputField.text : string.Empty;
            }

            set
            {
                if (roomNameInputField)
                    roomNameInputField.text = value;
            }
        }

        public string MaxConnections
        {
            get
            {
                return roomMaxConnectionsInputField != null ? roomMaxConnectionsInputField.text : string.Empty;
            }

            set
            {
                if (roomMaxConnectionsInputField)
                    roomMaxConnectionsInputField.text = value;
            }
        }

        public string RegionName
        {
            get
            {
                return regionNameInputField != null ? regionNameInputField.text : string.Empty;
            }

            set
            {
                if (regionNameInputField)
                    regionNameInputField.text = value;
            }
        }
    }
}