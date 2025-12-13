using UnityEngine;

namespace SmallAmbitions
{
    public enum IKTargetType
    {
        IK_None,
        IK_Pelvis,
        IK_LeftHand,
        IK_RightHand,
    }

    [System.Serializable]
    public struct IKTarget
    {
        [field: SerializeField] public IKTargetType Type { get; private set; }
        [field: SerializeField] public Transform Target { get; private set; }

        public Vector3 Position => Target.position;
        public Quaternion Rotation => Target.rotation;

        public Vector3 LocalPosition => Target.localPosition;
        public Quaternion LocalRotation => Target.localRotation;

        public Transform Transform => Target;

        public IKTarget(IKTargetType type, Transform target)
        {
            Type = type;
            Target = target;
        }

        public bool IsValid()
        {
            return Target != null && Type != IKTargetType.IK_None;
        }
    }
}
