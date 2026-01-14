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
        [SerializeField] private UER _response;

        private void OnEnable()
        {
            if (_event != null)
            {
                _event.RegisterListener(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.UnregisterListener(OnEventRaised);
            }
        }

        private void OnEventRaised(T value)
        {
            (_response as UnityEvent<T>)?.Invoke(value);
        }
    }

    [Serializable]
    public sealed class VoidUnityEvent : UnityEvent<Void>
    { }

    public sealed class GameEventListener : GameEventListener<Void, GameEvent, VoidUnityEvent>
    { }
}
