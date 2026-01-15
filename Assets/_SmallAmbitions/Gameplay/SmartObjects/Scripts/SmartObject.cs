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

        public IReadOnlyList<Interaction> Interactions => _interactions;
        public IReadOnlyList<InteractionSlotDefinition> InteractionSlots => _interactionSlots;

        private void Awake()
        {
            BuildSlotInstances();
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
            _slotInstances = _interactionSlots.ConvertAll(def => new InteractionSlotInstance(def));
        }

        private List<InteractionSlotInstance> FindMatchingSlots(IReadOnlyCollection<InteractionSlotType> requiredSlotTypes)
        {
            var matched = new List<InteractionSlotInstance>(requiredSlotTypes.Count);

            foreach (var requiredType in requiredSlotTypes)
            {
                var found = _slotInstances.FirstOrDefault(slot => !matched.Contains(slot) && slot.HasSlotType(requiredType) && slot.IsAvailable());

                if (found == null)
                {
                    return null;
                }

                matched.Add(found);
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
            foreach (var slot in _slotInstances)
            {
                if (!slot.HasSlotType(InteractionSlotType.StandPosition))
                    continue;

                if (slot.IsReservedBy(user))
                {
                    standTransform = slot.SlotTransform;
                    return true;
                }
            }

            standTransform = null;
            return false;
        }
    }
}
