﻿using Barebones.Networking;
using System.Collections.Generic;

namespace Barebones.MasterServer
{
    public class GameInfoPacket : SerializablePacket
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public GameInfoType Type { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public bool IsPasswordProtected { get; set; }
        public int MaxPlayers { get; set; }
        public int OnlinePlayers { get; set; }
        public DictionaryOptions CustomOptions { get; set; }

        public GameInfoPacket()
        {
            Id = 0;
            Address = string.Empty;
            Name = string.Empty;
            Region = string.Empty;
            Type = GameInfoType.Unknown;
            IsPasswordProtected = false;
            MaxPlayers = 0;
            OnlinePlayers = 0;
            CustomOptions = new DictionaryOptions();
        }

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Address);
            writer.Write((int)Type);
            writer.Write(Name);
            writer.Write(Region);

            writer.Write(IsPasswordProtected);
            writer.Write(MaxPlayers);
            writer.Write(OnlinePlayers);
            writer.Write(CustomOptions.ToDictionary());
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Id = reader.ReadInt32();
            Address = reader.ReadString();
            Type = (GameInfoType)reader.ReadInt32();
            Name = reader.ReadString();
            Region = reader.ReadString();

            IsPasswordProtected = reader.ReadBoolean();
            MaxPlayers = reader.ReadInt32();
            OnlinePlayers = reader.ReadInt32();
            CustomOptions = new DictionaryOptions(reader.ReadDictionary());
        }

        public override string ToString()
        {
            string maxPleyers = MaxPlayers <= 0 ? "Unlimited" : MaxPlayers.ToString();

            var options = new DictionaryOptions();
            options.Add("Id", Id);
            options.Add("Address", Address);
            options.Add("Type", Type.ToString());
            options.Add("Name", Name);
            options.Add("Region", string.IsNullOrEmpty(Region) ? "International" : Region);
            options.Add("IsPasswordProtected", IsPasswordProtected);
            options.Add("MaxPlayers", maxPleyers);
            options.Add("OnlinePlayers", $"{OnlinePlayers}/{maxPleyers}");
            options.Append(CustomOptions);

            return $"[GameInfo: {options.ToReadableString()}]";
        }
    }
}