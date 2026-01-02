using System;
using UnityEngine;

namespace SmallAmbitions
{
    [Serializable]
    public sealed class EnumIndexedArray<TEnum, TValue> : ISerializationCallbackReceiver where TEnum : unmanaged, Enum
    {
        [Serializable]
        private struct Entry
        {
            [field: SerializeField] public TEnum Key { get; private set; }
            [field: SerializeField] public TValue Value { get; private set; }
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private TValue[] _values = Array.Empty<TValue>();
        private bool[] _occupied = Array.Empty<bool>();

        #region ISerializationCallbackReceiver

        public void OnBeforeSerialize()
        { /* Intentionally empty */ }

        public void OnAfterDeserialize()
        {
            var enumValues = Enum.GetValues(typeof(TEnum));
            int enumCount = enumValues.Length;

            int minValue = int.MaxValue;
            int maxValue = int.MinValue;

            foreach (TEnum value in enumValues)
            {
                int intValue = Convert.ToInt32(value);
                minValue = Math.Min(minValue, intValue);
                maxValue = Math.Max(maxValue, intValue);
            }

            if (minValue != 0 || maxValue != enumCount - 1)
            {
                Debug.LogError($"EnumIndexedArray requires {typeof(TEnum).Name} to be dense and zero-based " +
                               $"(values 0 to {enumCount - 1}). Found range [{minValue}, {maxValue}]. " +
                               $"This container will not function correctly.");
                _values = Array.Empty<TValue>();
                _occupied = Array.Empty<bool>();
                return;
            }

            _values = new TValue[enumCount];
            _occupied = new bool[enumCount];

            for (int i = 0; i < _entries.Length; ++i)
            {
                var entry = _entries[i];
                int index = Convert.ToInt32(entry.Key);

                if (_occupied[index])
                {
                    Debug.LogError($"Duplicate key '{entry.Key}' found in EnumIndexedArray. " +
                                   $"Each key must appear at most once. Fix the authored data.");
                    continue;
                }

                _values[index] = entry.Value;
                _occupied[index] = true;
            }
        }

        #endregion ISerializationCallbackReceiver

        public TValue this[TEnum key]
        {
            get
            {
                int slotIndex = Convert.ToInt32(key);
                return _values[slotIndex];
            }
        }

        public bool ContainsKey(TEnum key)
        {
            int index = Convert.ToInt32(key);
            return _occupied[index];
        }

        public bool TryGetValue(TEnum key, out TValue value)
        {
            int index = Convert.ToInt32(key);

            if (_occupied[index])
            {
                value = _values[index];
                return true;
            }

            value = default;
            return false;
        }
    }
}
