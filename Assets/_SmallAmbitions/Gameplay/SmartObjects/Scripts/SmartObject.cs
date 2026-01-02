using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class SmartObject : MonoBehaviour
    {
        [SerializeField] private List<InteractionSlotDefinition> _interactionSlots = new();
        [SerializeField] private SerializableSet<Interaction> _interactions = new();
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;

        private List<InteractionSlotInstance> _slotInstances;

        public IReadOnlyList<Interaction> Interactions => _interactions;
        public IReadOnlyList<InteractionSlotDefinition> InteractionSlots => _interactionSlots;

        private void Awake()
        {
            BuildSlotInstances();
        }

        private void OnEnable()
        {
            _smartObjects.Add(this);
        }

        private void OnDisable()
        {
            _smartObjects.Remove(this);
        }

        private void BuildSlotInstances()
        {
            _slotInstances = new List<InteractionSlotInstance>(_interactionSlots.Count);
            foreach (var slotDefinition in _interactionSlots)
            {
                _slotInstances.Add(new InteractionSlotInstance(slotDefinition));
            }
        }

        public bool HasAvailableSlots(IReadOnlyCollection<InteractionSlotType> requiredSlotTypes)
        {
            var used = new HashSet<InteractionSlotInstance>();

            foreach (var requiredSlotType in requiredSlotTypes)
            {
                var foundSlot = false;
                foreach (var slot in _slotInstances)
                {
                    if (used.Contains(slot))
                    {
                        continue;
                    }

                    if (slot.HasSlotType(requiredSlotType) && slot.IsAvailable())
                    {
                        used.Add(slot);
                        foundSlot = true;
                        break;
                    }
                }

                if (!foundSlot)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryReserveSlots(IReadOnlyCollection<InteractionSlotType> requiredSlotTypes, GameObject user)
        {
            var slotsToReserve = new List<InteractionSlotInstance>();

            foreach (var requiredSlotType in requiredSlotTypes)
            {
                InteractionSlotInstance foundSlot = null;

                foreach (var slot in _slotInstances)
                {
                    if (slotsToReserve.Contains(slot))
                    {
                        continue;
                    }

                    if (slot.HasSlotType(requiredSlotType) && slot.IsAvailable())
                    {
                        foundSlot = slot;
                        break;
                    }
                }

                if (foundSlot == null)
                {
                    return false;
                }

                slotsToReserve.Add(foundSlot);
            }

            foreach (var slot in slotsToReserve)
            {
                slot.RegisterUser(user);
            }

            return true;
        }

        public void ReleaseSlots(GameObject user)
        {
            foreach (var slot in _slotInstances)
            {
                slot.UnregisterUser(user);
            }
        }

        public bool TryGetAvailableStandPosition(out Transform standTransform)
        {
            foreach (var slot in _slotInstances)
            {
                if (!slot.HasSlotType(InteractionSlotType.StandPosition))
                    continue;

                if (!slot.IsAvailable())
                    continue;

                standTransform = slot.SlotTransform;
                return true;
            }

            standTransform = null;
            return false;
        }
    }
}
