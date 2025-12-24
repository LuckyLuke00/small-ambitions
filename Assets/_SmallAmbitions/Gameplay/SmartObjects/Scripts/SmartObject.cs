using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class SmartObject : MonoBehaviour
    {
        [SerializeField] private SerializableMap<InteractionSlotType, Transform> _interactionSlots = new();

        public IReadOnlyDictionary<InteractionSlotType, Transform> InteractionSlots => _interactionSlots;
    }
}
