using System;
using UnityEngine;

namespace SmallAmbitions
{
    public abstract class GameEvent<T> : ScriptableObject
    {
        private event Action<T> _event;

        public void Raise(T value) => _event?.Invoke(value);

        public void RegisterListener(Action<T> callback) => _event += callback;

        public void UnregisterListener(Action<T> callback) => _event -= callback;
    }

    public readonly struct Void
    { }

    [CreateAssetMenu(menuName = "Small Ambitions/Game Events/Game Event", fileName = "GameEvent")]
    public sealed class GameEvent : GameEvent<Void>
    {
        public void Raise() => Raise(new Void());
    }
}
