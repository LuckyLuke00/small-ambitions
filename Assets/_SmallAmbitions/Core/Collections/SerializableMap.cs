using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [Serializable]
    public sealed class SerializableMap<TKey, TValue> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        [Serializable]
        private struct Entry
        {
            public TKey Key;
            public TValue Value;
        }

        [SerializeField] private readonly List<Entry> _entries = new();
        private readonly Dictionary<TKey, TValue> _dictionary = new();

        #region ISerializationCallbackReceiver

        public void OnBeforeSerialize()
        { /* Intentionally empty: duplicates in _entries are resolved in OnAfterDeserialize via TryAdd */ }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();
            _dictionary.EnsureCapacity(_entries.Count);

            foreach (var entry in _entries)
            {
                _dictionary.TryAdd(entry.Key, entry.Value);
            }
        }

        #endregion ISerializationCallbackReceiver

        #region IEnumerable<KeyValuePair<TKey, TValue>>

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable<KeyValuePair<TKey, TValue>>
    }
}
