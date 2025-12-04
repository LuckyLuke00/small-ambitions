using UnityEngine;
using UnityEngine.Events;

namespace SmallAmbitions
{
    public class GameEventListener<T> : MonoBehaviour
    {
        [SerializeField] private GameEvent<T> _event;
        [SerializeField] private UnityEvent<T> _response;

        private void OnEnable()
        {
            _event.RegisterListener(this);
        }

        private void OnDisable()
        {
            _event.UnregisterListener(this);
        }

        public void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }
}
