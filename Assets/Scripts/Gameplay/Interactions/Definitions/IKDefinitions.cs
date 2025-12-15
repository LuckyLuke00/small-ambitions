using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SmallAmbitions
{
    public enum IKTargetType
    {
        None,
        Pelvis,
        LeftHand,
        RightHand,
    }

    [System.Serializable]
    public struct IKTarget
    {
        [field: SerializeField] public IKTargetType Type { get; private set; }
        [field: SerializeField] public Transform Transform { get; private set; }

        public IKTarget(IKTargetType type, Transform transform)
        {
            Type = type;
            Transform = transform;
        }

        public bool IsValid()
        {
            return Transform != null && Type != IKTargetType.None;
        }
    }

    [System.Serializable]
    public sealed class IKRig
    {
        [field: SerializeField] public Rig Rig { get; private set; }
        [field: SerializeField] public Transform IKTarget { get; private set; }

        [field: SerializeField, Range(0f, 1f)] public float TargetWeight { get; private set; } = 1f;
        [field: SerializeField, Range(0f, 1f)] public float DefaultWeight { get; private set; } = 0f;
        [field: SerializeField] public float BlendInSpeed { get; private set; } = 2f;
        [field: SerializeField] public float BlendOutSpeed { get; private set; } = 2f;

        [field: System.NonSerialized] public Coroutine ActiveRoutine;

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
