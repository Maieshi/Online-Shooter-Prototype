using Aevien.Utilities;
using Barebones.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicProfile
{
    public enum ObservablePropertiyCodes { DisplayName, Avatar, Bronze, Silver, Gold }

    public class CustomProfilesModule : ProfilesModule
    {
        [Header("Start Values"), SerializeField]
        private float bronze = 100;
        [SerializeField]
        private float silver = 50;
        [SerializeField]
        private float gold = 50;

        public HelpBox _header = new HelpBox()
        {
            Text = "This script is a custom module, which sets up profiles values for new users"
        };

        public override void Initialize(IServer server)
        {
            base.Initialize(server);

            // Set the new factory in ProfilesModule
            ProfileFactory = CreateProfileInServer;

            server.SetHandler((short)MsfMessageCodes.UpdateDisplayNameRequest, UpdateDisplayNameRequestHandler);

            //Update profile resources each 5 sec
            InvokeRepeating(nameof(IncreaseResources), 1f, 1f);
        }

        private ObservableServerProfile CreateProfileInServer(string username, IPeer clientPeer)
        {
            return new ObservableServerProfile(username, clientPeer)
            {
                new ObservableString((short)ObservablePropertiyCodes.DisplayName, SimpleNameGenerator.Generate(Gender.Male)),
                new ObservableString((short)ObservablePropertiyCodes.Avatar, "https://i.imgur.com/JQ9pRoD.png"),
                new ObservableFloat((short)ObservablePropertiyCodes.Bronze, bronze),
                new ObservableFloat((short)ObservablePropertiyCodes.Silver, silver),
                new ObservableFloat((short)ObservablePropertiyCodes.Gold, gold)
            };
        }

        private void IncreaseResources()
        {
            foreach (var profile in ProfilesList.Values)
            {
                var bronzeProperty = profile.GetProperty<ObservableFloat>((short)ObservablePropertiyCodes.Bronze);
                var silverProperty = profile.GetProperty<ObservableFloat>((short)ObservablePropertiyCodes.Silver);
                var goldProperty = profile.GetProperty<ObservableFloat>((short)ObservablePropertiyCodes.Gold);

                bronzeProperty.Add(1f);
                silverProperty.Add(0.1f);
                goldProperty.Add(0.01f);
            }
        }

        private void UpdateDisplayNameRequestHandler(IIncommingMessage message)
        {
            var userExtension = message.Peer.GetExtension<IUserPeerExtension>();

            if (userExtension == null || userExtension.Account == null)
            {
                message.Respond("Invalid session", ResponseStatus.Unauthorized);
                return;
            }

            var newProfileData = new Dictionary<string, string>().FromBytes(message.AsBytes());

            try
            {
                if (ProfilesList.TryGetValue(userExtension.Username, out ObservableServerProfile profile))
                {
                    profile.GetProperty<ObservableString>((short)ObservablePropertiyCodes.DisplayName).Set(newProfileData["displayName"]);
                    profile.GetProperty<ObservableString>((short)ObservablePropertiyCodes.Avatar).Set(newProfileData["avatarUrl"]);

                    message.Respond(ResponseStatus.Success);
                }
                else
                {
                    message.Respond("Invalid session", ResponseStatus.Unauthorized);
                }
            }
            catch (Exception e)
            {
                message.Respond($"Internal Server Error: {e}", ResponseStatus.Error);
            }
        }
    }
}
