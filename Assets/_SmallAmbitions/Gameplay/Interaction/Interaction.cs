using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [CreateAssetMenu(fileName = "Interaction", menuName = "Small Ambitions/Interactions/New Interaction")]
    public sealed class Interaction : ScriptableObject
    {
        [SerializeField] private SerializableMap<MotiveType, float> _motiveDecayRates;
        public IReadOnlyDictionary<MotiveType, float> MotiveDecayRates => _motiveDecayRates;

        [field: SerializeField] public List<InteractionStep> StartSteps { get; private set; } = new();
        [field: SerializeField] public List<InteractionStep> LoopSteps { get; private set; } = new();
        [field: SerializeField] public List<InteractionStep> ExitSteps { get; private set; } = new();

        [field: SerializeField] public Interaction RequiredAmbientInteraction { get; private set; }

        [Tooltip("Must be resolved on the SmartObject offering the interaction.")]
        [field: SerializeField] public SerializableSet<InteractionSlotType> RequiredPrimarySlots { get; private set; } = new();

        [Tooltip("Must be resolved from any SmartObject in range.")]
        [field: SerializeField] public SerializableSet<InteractionSlotType> RequiredAmbientSlots { get; private set; } = new();
        [field: SerializeField, Range(0f, float.MaxValue)] public float PositionToleranceRadius { get; private set; } = 0f;
    }
}
