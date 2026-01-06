using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public abstract class RuntimeSet<T> : ScriptableObject, IEnumerable<T> where T : Component
    {
        private readonly HashSet<T> _items = new();

        public int Count => _items.Count;

        public void Add(T item) => _items.Add(item);

        public void Remove(T item) => _items.Remove(item);

        public void Clear() => _items.Clear();

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T FindClosest(Vector3 position)
        {
            T closest = null;
            float minSqrDistance = float.MaxValue;

            foreach (T item in _items)
            {
                if (item == null)
                {
                    continue;
                }

                float sqrDistance = (item.transform.position - position).sqrMagnitude;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest = item;
                }
            }

            return closest;
        }

        public T GetRandom()
        {
            return _items.GetRandomElement();
        }
    }
}
