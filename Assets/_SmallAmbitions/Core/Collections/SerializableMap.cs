using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [Serializable]
    public sealed class SerializableMap<TKey, TValue> : ISerializationCallbackReceiver, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        [Serializable]
        private struct Entry
        {
            public TKey Key;
            public TValue Value;
        }

        [SerializeField] private List<Entry> _entries = new();
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

        #region IReadOnlyDictionary<TKey, TValue>

        public TValue this[TKey key] => _dictionary[key];
        public IEnumerable<TKey> Keys => _dictionary.Keys;
        public IEnumerable<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IReadOnlyDictionary<TKey, TValue>
    }
}
