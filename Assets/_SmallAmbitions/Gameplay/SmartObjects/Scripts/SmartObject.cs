using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmallAmbitions
{
    public readonly struct TransformSnapshot
    {
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public TransformSnapshot(Transform transform)
        {
            Parent = transform.parent;
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
        }

        public void Restore(Transform transform)
        {
            transform.SetParent(Parent, worldPositionStays: false);
            transform.localPosition = LocalPosition;
            transform.localRotation = LocalRotation;
            transform.localScale = LocalScale;
        }
    }

    public sealed class SmartObject : MonoBehaviour
    {
        [SerializeField] private List<InteractionSlotDefinition> _interactionSlots = new();
        [SerializeField] private SerializableSet<Interaction> _interactions = new();
        [SerializeField] private GameEvent _slotsReleasedEvent;
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;
        [field: SerializeField] public GameObject AttachmentObject { get; private set; }
        private TransformSnapshot _attachmentOriginalPose;

        private List<InteractionSlotInstance> _slotInstances;
        private Dictionary<InteractionSlotType, List<InteractionSlotInstance>> _slotsByType;

        public IReadOnlyList<Interaction> Interactions => _interactions;
        public IReadOnlyList<InteractionSlotDefinition> InteractionSlots => _interactionSlots;

        private void Awake()
        {
            BuildSlotInstances(); // Now builds cache too

            if (AttachmentObject != null)
            {
                _attachmentOriginalPose = new TransformSnapshot(AttachmentObject.transform);
            }
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
            _slotsByType = new Dictionary<InteractionSlotType, List<InteractionSlotInstance>>();

            foreach (var def in _interactionSlots)
            {
                var instance = new InteractionSlotInstance(def);
                _slotInstances.Add(instance);

                // Build cache at the same time
                var slotType = def.SlotType;
                if (slotType != InteractionSlotType.None)
                {
                    if (!_slotsByType.ContainsKey(slotType))
                    {
                        _slotsByType[slotType] = new List<InteractionSlotInstance>();
                    }
                    _slotsByType[slotType].Add(instance);
                }
            }
        }
        private List<InteractionSlotInstance> FindMatchingSlots(IReadOnlyCollection<InteractionSlotType> requiredSlotTypes)
        {
            if (requiredSlotTypes == null || requiredSlotTypes.Count == 0)
            {
                return new List<InteractionSlotInstance>();
            }

            var matched = new List<InteractionSlotInstance>(requiredSlotTypes.Count);
            var matchedSet = new HashSet<InteractionSlotInstance>();

            foreach (var requiredType in requiredSlotTypes)
            {
                if (!_slotsByType.TryGetValue(requiredType, out var candidates))
                {
                    return null;
                }

                InteractionSlotInstance found = null;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var slot = candidates[i];
                    if (!matchedSet.Contains(slot) && slot.IsAvailable())
                    {
                        found = slot;
                        break;
                    }
                }

                if (found == null)
                {
                    return null;
                }

                matched.Add(found);
                matchedSet.Add(found);
            }

            return matched;
        }

        public bool HasAvailableSlots(IReadOnlyCollection<InteractionSlotType> requiredSlotTypes)
        {
            return FindMatchingSlots(requiredSlotTypes) != null;
        }

        public bool TryReserveSlots(IReadOnlyCollection<InteractionSlotType> requiredSlotTypes, GameObject user)
        {
            var slots = FindMatchingSlots(requiredSlotTypes);
            if (slots == null)
            {
                return false;
            }

            foreach (var slot in slots)
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

            _slotsReleasedEvent?.Raise();
        }

        public bool TryGetAvailableStandPosition(out Transform standTransform)
        {
            // Use cache for direct lookup
            if (_slotsByType.TryGetValue(InteractionSlotType.StandPosition, out var standSlots))
            {
                for (int i = 0; i < standSlots.Count; i++)
                {
                    if (standSlots[i].IsAvailable())
                    {
                        standTransform = standSlots[i].SlotTransform;
                        return true;
                    }
                }
            }

            standTransform = null;
            return false;
        }

        public void ResetAttachmentObjectTransform()
        {
            if (AttachmentObject == null)
            {
                return;
            }

            _attachmentOriginalPose.Restore(AttachmentObject.transform);
        }

        public void AttachAttachmentObject(Transform parentTransform)
        {
            if (AttachmentObject == null || parentTransform == null)
            {
                return;
            }

            var t = AttachmentObject.transform;
            t.SetParent(parentTransform, worldPositionStays: true);
        }

        public bool TryGetStandPositionForUser(GameObject user, out Transform standTransform)
        {
            if (_slotsByType.TryGetValue(InteractionSlotType.StandPosition, out var standSlots))
            {
                for (int i = 0; i < standSlots.Count; ++i)
                {
                    if (standSlots[i].IsReservedBy(user))
                    {
                        standTransform = standSlots[i].SlotTransform;
                        return true;
                    }
                }
            }

            standTransform = null;
            return false;
        }
    }
}
