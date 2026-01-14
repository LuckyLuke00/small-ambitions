using System;
using UnityEngine;
using UnityEngine.Events;

namespace SmallAmbitions
{
    public abstract class GameEventListener<T, E, UER> : MonoBehaviour
        where E : GameEvent<T>
        where UER : UnityEventBase
    {
        [SerializeField] private E _event;
        [SerializeField] protected UER _response;

        private void OnEnable()
        {
            _event?.RegisterListener(OnEventRaised);
        }

        private void OnDisable()
        {
            _event?.UnregisterListener(OnEventRaised);
        }

        protected abstract void OnEventRaised(T value);
    }

    [Serializable]
    public sealed class VoidUnityEvent : UnityEvent<Void>
    { }

    public sealed class GameEventListener : GameEventListener<Void, GameEvent, VoidUnityEvent>
    {
        protected override void OnEventRaised(Void value)
        {
            _response?.Invoke(value);
        }
    }
}
