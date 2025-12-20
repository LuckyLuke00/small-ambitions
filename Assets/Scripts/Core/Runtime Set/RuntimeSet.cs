using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public abstract class RuntimeSet<T> : ScriptableObject where T : Component
    {
        [SerializeField] private List<T> _items = new List<T>();

        public void Add(T item)
        {
            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        public void Remove(T item)
        {
            if (_items.Contains(item))
            {
                _items.Remove(item);
            }
        }

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
            if (!Utils.IsNullOrEmpty(_items))
            {
                return null;
            }

            return _items[Random.Range(0, _items.Count)];
        }
    }
}
