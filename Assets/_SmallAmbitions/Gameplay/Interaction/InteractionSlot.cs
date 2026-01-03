using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SmallAmbitions
{
    public enum InteractionSlotType
    {
        None = 0,
        StandPosition,
        LeftHand,
        RightHand,
        Pelvis,
        LookAt
    }

    public enum InteractionSlotPolicy
    {
        SingleUse = 0,
        MultiUse
    }

    [Serializable]
    public struct InteractionSlotDefinition
    {
        [field: SerializeField] public InteractionSlotType SlotType { get; private set; }
        [field: SerializeField] public InteractionSlotPolicy SlotPolicy { get; private set; }
        [field: SerializeField] public Transform SlotTransform { get; private set; }
    }

    public sealed class InteractionSlotInstance
    {
        private InteractionSlotDefinition _slotDefinition;
        private HashSet<GameObject> _currentUsers = new HashSet<GameObject>();

        public Transform SlotTransform => _slotDefinition.SlotTransform;

        public InteractionSlotInstance(InteractionSlotDefinition slotDefinition) => _slotDefinition = slotDefinition;

        public bool IsAvailable() => _slotDefinition.SlotPolicy == InteractionSlotPolicy.MultiUse || _currentUsers.Count == 0;

        public bool HasSlotType(InteractionSlotType slotType) => _slotDefinition.SlotType == slotType;

        public void RegisterUser(GameObject user)
        {
            _currentUsers.Add(user);
        }

        public void UnregisterUser(GameObject user)
        {
            _currentUsers.Remove(user);
        }
    }

    [System.Serializable]
    public sealed class IKRig
    {
        [field: SerializeField] public Rig Rig { get; private set; }
        [field: SerializeField] public Transform IKTarget { get; private set; }
        [field: SerializeField] public Transform AttachmentPoint { get; private set; }

        public float Weight
        {
            get => Rig.weight;
            set => Rig.weight = value;
        }

        public void MoveIKTarget(Transform target)
        {
            IKTarget.position = target.position;
            IKTarget.rotation = target.rotation;
        }
    }
}
