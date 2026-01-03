using System;
using UnityEngine;

namespace SmallAmbitions
{
    [Serializable]
    public struct InteractionStep
    {
        [field: SerializeField, Min(0f)] public float DurationSeconds { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float TargetRigWeight { get; private set; }

        [field: SerializeField] public bool ResetAttachement { get; private set; }
        [field: SerializeField] public AnimationClip AnimationToPlay { get; private set; }
        [field: SerializeField] public InteractionSlotType AttachToSlot { get; private set; }
    }
}
