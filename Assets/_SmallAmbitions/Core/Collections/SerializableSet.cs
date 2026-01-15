using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [Serializable]
    public sealed class SerializableSet<T> : ISerializationCallbackReceiver, IReadOnlyList<T>
    {
        [SerializeField] private T[] _entries = Array.Empty<T>();
        private T[] _values = Array.Empty<T>();

        #region ISerializationCallbackReceiver

        public void OnBeforeSerialize()
        { /* Intentionally empty */ }

        public void OnAfterDeserialize()
        {
            int count = _entries.Length;

            if (count == 0)
            {
                _values = Array.Empty<T>();
                return;
            }

            _values = new T[count];
            Array.Copy(_entries, _values, count);
        }

        #endregion ISerializationCallbackReceiver

        public bool Contains(T value)
        {
            for (int i = 0; i < _values.Length; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(_values[i], value))
                    return true;
            }
            return false;
        }

        #region IReadOnlyList<T>

        public int Count => _values.Length;
        public T this[int index] => _values[index];

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IReadOnlyList<T>
    }
}
