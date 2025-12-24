using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class SmartObject : MonoBehaviour
    {
        [SerializeField] private SerializableMap<InteractionSlotType, Transform> _interactionSlots = new();
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;

        public IReadOnlyDictionary<InteractionSlotType, Transform> InteractionSlots => _interactionSlots;

        private void OnEnable()
        {
            _smartObjects.Add(this);
        }

        private void OnDisable()
        {
            _smartObjects.Remove(this);
        }
    }
}
