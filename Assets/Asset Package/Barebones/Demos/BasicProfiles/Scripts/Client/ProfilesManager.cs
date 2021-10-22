﻿using Aevien.UI;
using Barebones.MasterServer.Examples.BasicProfile;
using Barebones.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Barebones.MasterServer.Examples.BasicProfile
{
    public class ProfilesManager : MsfBaseClientModule
    {
        public ObservableProfile Profile { get; private set; }

        private ProfileView profileView;
        private ProfileSettingsView profileSettingsView;

        public event Action<short, IObservableProperty> OnPropertyUpdatedEvent;
        public UnityEvent OnProfileLoadedEvent;
        public UnityEvent OnProfileSavedEvent;

        protected override void OnBeforeClientConnectedToServer()
        {
            profileView = ViewsManager.GetView<ProfileView>("ProfileView");
            profileSettingsView = ViewsManager.GetView<ProfileSettingsView>("ProfileSettingsView");

            Profile = new ObservableProfile
            {
                new ObservableString((short)ObservablePropertiyCodes.DisplayName),
                new ObservableString((short)ObservablePropertiyCodes.Avatar),
                new ObservableFloat((short)ObservablePropertiyCodes.Bronze),
                new ObservableFloat((short)ObservablePropertiyCodes.Silver),
                new ObservableFloat((short)ObservablePropertiyCodes.Gold)
            };

            Profile.OnPropertyUpdatedEvent += OnPropertyUpdatedEventHandler;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Profile.OnPropertyUpdatedEvent -= OnPropertyUpdatedEventHandler;
        }

        private void OnPropertyUpdatedEventHandler(short key, IObservableProperty property)
        {
            OnPropertyUpdatedEvent?.Invoke(key, property);

            logger.Debug($"Property with code: {key} were updated: {property.Serialize()}");
        }

        public void LoadProfile()
        {
            Msf.Events.Invoke(EventKeys.showLoadingInfo, "Loading profile... Please wait!");

            MsfTimer.WaitForSeconds(1f, () =>
            {
                Msf.Client.Profiles.GetProfileValues(Profile, (isSuccessful, error) =>
                {
                    if (isSuccessful)
                    {
                        Msf.Events.Invoke(EventKeys.hideLoadingInfo);
                        OnProfileLoadedEvent?.Invoke();
                    }
                    else
                    {
                        Msf.Events.Invoke(EventKeys.showOkDialogBox,
                            new OkDialogBoxViewEventMessage($"When requesting profile data an error occurred. [{error}]"));
                    }
                });
            });
        }

        public void UpdateProfile()
        {
            Msf.Events.Invoke(EventKeys.showLoadingInfo, "Saving profile data... Please wait!");

            MsfTimer.WaitForSeconds(1f, () =>
            {
                var data = new Dictionary<string, string>
                {
                    { "displayName", profileSettingsView.DisplayName },
                    { "avatarUrl", profileSettingsView.AvatarUrl }
                };

                Connection.SendMessage((short)MsfMessageCodes.UpdateDisplayNameRequest, data.ToBytes(), OnSaveProfileResponseCallback);
            });
        }

        private void OnSaveProfileResponseCallback(ResponseStatus status, IIncommingMessage response)
        {
            Msf.Events.Invoke(EventKeys.hideLoadingInfo);

            if(status == ResponseStatus.Success)
            {
                OnProfileSavedEvent?.Invoke();

                logger.Debug("Your profile is successfuly updated and saved");
            }
            else
            {
                Msf.Events.Invoke(EventKeys.showOkDialogBox, new OkDialogBoxViewEventMessage(response.AsString()));
                logger.Error(response.AsString());
            }
        }
    }
}
