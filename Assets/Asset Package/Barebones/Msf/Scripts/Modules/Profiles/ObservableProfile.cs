using Barebones.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Barebones.MasterServer
{
    /// <summary>
    /// Represents clients profile, which emits events about changes.
    /// Client, game server and master servers will create a similar
    /// object.
    /// </summary>
    public class ObservableProfile : IEnumerable<IObservableProperty>
    {
        /// <summary>
        /// Profile properties list
        /// </summary>
        private Dictionary<short, IObservableProperty> _properties;

        /// <summary>
        /// Properties that are changed and waiting for to be sent
        /// </summary>
        private HashSet<IObservableProperty> _notBroadcastedProperties;

        public delegate void PropertyUpdateHandler(short propertyCode, IObservableProperty property);

        /// <summary>
        /// 
        /// </summary>
        public HashSet<IObservableProperty> UnsavedProperties { get; protected set; }

        /// <summary>
        /// Invoked, when one of the values changes
        /// </summary>
        public event Action<short, IObservableProperty> OnPropertyUpdatedEvent;

        /// <summary>
        /// Invoked, when something in the profile changes
        /// </summary>
        public event Action<ObservableProfile> OnModifiedEvent;

        public ObservableProfile()
        {
            _properties = new Dictionary<short, IObservableProperty>();
            _notBroadcastedProperties = new HashSet<IObservableProperty>();
            UnsavedProperties = new HashSet<IObservableProperty>();
        }

        /// <summary>
        /// Check if profile has changed properties
        /// </summary>
        public bool HasDirtyProperties { get { return _notBroadcastedProperties.Count > 0; } }

        /// <summary>
        /// The number of properties the profile has
        /// </summary>
        public int PropertyCount { get { return _properties.Count; } }

        /// <summary>
        /// Returns an observable value of given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetProperty<T>(short key) where T : class, IObservableProperty
        {
            return _properties[key].CastTo<T>();
        }

        /// <summary>
        /// Returns an observable value
        /// </summary>
        public IObservableProperty GetProperty(short key)
        {
            return _properties[key];
        }

        /// <summary>
        /// Tries get propfile property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetProperty<T>(short key, out T result) where T : class, IObservableProperty
        {
            bool getResult = _properties.TryGetValue(key, out IObservableProperty val);
            result = val as T;
            return getResult;
        }

        /// <summary>
        /// Adds a value to profile
        /// </summary>
        /// <param name="property"></param>
        public void AddProperty(IObservableProperty property)
        {
            _properties.Add(property.Key, property);
            property.OnDirtyEvent += OnDirtyProperty;
        }

        /// <summary>
        /// Adds property to current profile
        /// </summary>
        /// <param name="property"></param>
        public void Add(IObservableProperty property)
        {
            AddProperty(property);
        }

        /// <summary>
        /// Called, when a value becomes dirty
        /// </summary>
        /// <param name="property"></param>
        protected virtual void OnDirtyProperty(IObservableProperty property)
        {
            _notBroadcastedProperties.Add(property);
            UnsavedProperties.Add(property);

            // TODO Possible optimisation, by not invoking OnChanged event.
            // Need to do more research, if this event is necessary to be invoked
            // on every change
            OnModifiedEvent?.Invoke(this);

            OnPropertyUpdatedEvent?.Invoke(property.Key, property);
        }

        /// <summary>
        /// Writes all data from profile to buffer
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, stream))
                {
                    // Write count
                    writer.Write(_properties.Count);

                    foreach (var value in _properties)
                    {
                        // Write key
                        writer.Write(value.Key);

                        var data = value.Value.ToBytes();

                        // Write data length
                        writer.Write(data.Length);

                        // Write data
                        writer.Write(data);
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Restores profile from data in the buffer
        /// </summary>
        public void FromBytes(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    var count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var key = reader.ReadInt16();
                        var length = reader.ReadInt32();
                        var valueData = reader.ReadBytes(length);

                        if (!_properties.ContainsKey(key))
                        {
                            continue;
                        }

                        _properties[key].FromBytes(valueData);
                    }
                }
            }
        }

        /// <summary>
        /// Restores profile from dictionary of strings
        /// </summary>
        /// <param name="dataData"></param>
        public void FromStrings(Dictionary<short, string> dataData)
        {
            foreach (var pair in dataData)
            {
                IObservableProperty property;
                _properties.TryGetValue(pair.Key, out property);
                if (property != null)
                {
                    property.Deserialize(pair.Value);
                }
            }
        }

        /// <summary>
        /// Returns observable properties changes, writen to
        /// byte array
        /// </summary>
        /// <returns></returns>
        public byte[] GetUpdates()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    GetUpdates(writer);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Writes changes into the writer
        /// </summary>
        /// <param name="writer"></param>
        public void GetUpdates(EndianBinaryWriter writer)
        {
            // Write values count
            writer.Write(_notBroadcastedProperties.Count);

            foreach (var property in _notBroadcastedProperties)
            {
                // Write key
                writer.Write(property.Key);

                var updates = property.GetUpdates();

                // Write udpates length
                writer.Write(updates.Length);

                // Write actual updates
                writer.Write(updates);
            }
        }

        public void ClearUpdates()
        {
            foreach (var property in _notBroadcastedProperties)
            {
                property.ClearUpdates();
            }

            _notBroadcastedProperties.Clear();
        }

        /// <summary>
        /// Uses updates data to update values in the profile
        /// </summary>
        /// <param name="updates"></param>
        public void ApplyUpdates(byte[] updates)
        {
            using (var ms = new MemoryStream(updates))
            {
                using (var reader = new EndianBinaryReader(EndianBitConverter.Big, ms))
                {
                    ApplyUpdates(reader);
                }
            }
        }

        /// <summary>
        /// Use updates data to update values in the profile
        /// </summary>
        /// <param name="updates"></param>
        public void ApplyUpdates(EndianBinaryReader reader)
        {
            // Read count
            var count = reader.ReadInt32();

            var dataRead = new Dictionary<short, byte[]>(count);

            // Read data first, because, in case of an exception
            // we want the pointer of reader to be at the right place 
            // (at the end of current updates)
            for (var i = 0; i < count; i++)
            {
                // Read key
                var key = reader.ReadInt16();

                // Read length
                var dataLength = reader.ReadInt32();

                // Read update data
                var data = reader.ReadBytes(dataLength);

                if (!dataRead.ContainsKey(key))
                {
                    dataRead.Add(key, data);
                }
            }

            // Update observables
            foreach (var updateEntry in dataRead)
            {
                if (_properties.TryGetValue(updateEntry.Key, out IObservableProperty property))
                {
                    property.ApplyUpdates(updateEntry.Value);
                }
            }
        }

        /// <summary>
        /// Serializes all of the properties to short/string dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<short, string> ToStringsDictionary()
        {
            var dict = new Dictionary<short, string>();

            foreach (var pair in _properties)
            {
                dict.Add(pair.Key, pair.Value.Serialize());
            }

            return dict;
        }



        public IEnumerator<IObservableProperty> GetEnumerator()
        {
            return _properties.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
