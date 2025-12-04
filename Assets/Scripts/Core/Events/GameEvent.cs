using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public abstract class GameEvent<T> : ScriptableObject
    {
        private readonly List<GameEventListener<T>> _eventListeners = new List<GameEventListener<T>>();

        public void Raise(T value)
        {
            for (int i = _eventListeners.Count - 1; i >= 0; --i)
            {
                _eventListeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(GameEventListener<T> listener)
        {
            if (!_eventListeners.Contains(listener))
            {
                _eventListeners.Add(listener);
            }
        }

        public void UnregisterListener(GameEventListener<T> listener)
        {
            if (_eventListeners.Contains(listener))
            {
                _eventListeners.Remove(listener);
            }
        }
    }
}
