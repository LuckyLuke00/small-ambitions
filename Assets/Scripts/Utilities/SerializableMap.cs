using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [System.Serializable]
    public sealed class SerializableMap<TKey, TValue> : ISerializationCallbackReceiver
    {
        [System.Serializable]
        private struct Entry
        {
            public TKey Key;
            public TValue Value;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private readonly Dictionary<TKey, TValue> _lookup = new Dictionary<TKey, TValue>();
        private bool _isDirty;

        public Dictionary<TKey, TValue>.ValueCollection Values => _lookup.Values;

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _lookup.TryGetValue(key, out value);
        }

        public void OnBeforeSerialize()
        {
            if (!_isDirty)
                return;

            _entries.Clear();
            _entries.Capacity = _lookup.Count;

            foreach (var pair in _lookup)
            {
                _entries.Add(new Entry { Key = pair.Key, Value = pair.Value });
            }

            _isDirty = false;
        }

        public void OnAfterDeserialize()
        {
            _lookup.Clear();

            foreach (var entry in _entries)
            {
                _lookup.TryAdd(entry.Key, entry.Value);
            }
        }
    }
}
