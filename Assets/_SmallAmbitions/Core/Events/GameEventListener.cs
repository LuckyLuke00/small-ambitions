using UnityEngine;
using UnityEngine.Events;

namespace SmallAmbitions
{
    public abstract class GameEventListener<T> : MonoBehaviour
    {
        [SerializeField] private GameEvent<T> _event;
        [SerializeField] private UnityEvent<T> _response;

        private void OnEnable()
        {
            _event?.RegisterListener(OnEventRaised);
        }

        private void OnDisable()
        {
            _event?.UnregisterListener(OnEventRaised);
        }

        public void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }

    public sealed class GameEventListener : GameEventListener<Void>
    { }
}
