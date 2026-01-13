using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmallAmbitions
{
    public readonly struct AutonomyTarget
    {
        public Interaction Interaction { get; }
        public SmartObject PrimarySmartObject { get; }
        public SmartObject AmbientSmartObject { get; }

        public AutonomyTarget(Interaction interaction, SmartObject primarySmartObject, SmartObject ambientSmartObject)
        {
            Interaction = interaction;
            PrimarySmartObject = primarySmartObject;
            AmbientSmartObject = ambientSmartObject;
        }
    }

    public sealed class AutonomyController : MonoBehaviour
    {
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private MotiveComponent _motiveComponent;

        public AutonomyTarget CurrentAutonomyTarget { get; private set; }
        public bool HasReservedTarget { get; private set; }

        public bool AquireNewAutonomyTarget()
        {
            ReleaseCurrentTarget();

            if (!_interactionManager.TryGetAvailableInteractions(out List<InteractionCandidate> candidates))
            {
                Debug.LogWarning($"{nameof(AutonomyController)}: No Interaction Candidates found.");
                return false;
            }

            if (!TryReserveBestCandidate(candidates, out AutonomyTarget target))
            {
                Debug.LogWarning($"{nameof(AutonomyController)}: Failed to reserve any interaction candidate.");
                return false;
            }

            CurrentAutonomyTarget = target;
            HasReservedTarget = true;
            return true;
        }

        public void ReleaseCurrentTarget()
        {
            if (!HasReservedTarget)
            {
                return;
            }

            var target = CurrentAutonomyTarget;
            target.PrimarySmartObject?.ReleaseSlots(gameObject);
            target.AmbientSmartObject?.ReleaseSlots(gameObject);

            HasReservedTarget = false;
        }

        public void ConsumeReservation()
        {
            HasReservedTarget = false;
        }

        private void OnDisable()
        {
            ReleaseCurrentTarget();
        }

        private bool TryReserveBestCandidate(IReadOnlyList<InteractionCandidate> candidates, out AutonomyTarget target)
        {
            // Sort candidates by urgency-weighted score, try to reserve highest first
            var sortedCandidates = candidates.OrderByDescending(c => ScoreInteraction(c.Interaction));
            foreach (var candidate in sortedCandidates)
            {
                if (TryReserveCandidate(candidate, out target))
                {
                    return true;
                }
            }

            target = default;
            return false;
        }

        private bool TryReserveCandidate(InteractionCandidate candidate, out AutonomyTarget target)
        {
            var interaction = candidate.Interaction;
            var primaryObject = candidate.SmartObject;
            SmartObject ambientObject = null;

            if (candidate.CandidateAmbientSmartObjects.Count > 0)
            {
                ambientObject = TryFindClosestTo(candidate.CandidateAmbientSmartObjects, primaryObject.transform.position);
            }

            bool needsAmbient = interaction.RequiredAmbientSlots != null && interaction.RequiredAmbientSlots.Count > 0;

            // DON'T reserve RequiredAmbientSlots here - they're validated but not reserved
            if (needsAmbient)
            {
                if (ambientObject == null || ambientObject == primaryObject)
                {
                    target = default;
                    return false;
                }

                // Just validate they exist, don't reserve yet
                if (!ambientObject.HasAvailableSlots(interaction.RequiredAmbientSlots))
                {
                    target = default;
                    return false;
                }
            }

            // Reserve primary object slots
            if (interaction.RequiredPrimarySlots.Count > 0)
            {
                if (!primaryObject.TryReserveSlots(interaction.RequiredPrimarySlots, gameObject))
                {
                    target = default;
                    return false;
                }
            }

            // Reserve posture/ambient interaction slots
            if (interaction.RequiredAmbientInteraction != null)
            {
                SmartObject postureObject = ambientObject != null ? ambientObject : primaryObject;

                if (interaction.RequiredAmbientInteraction.RequiredPrimarySlots.Count > 0)
                {
                    if (!postureObject.TryReserveSlots(interaction.RequiredAmbientInteraction.RequiredPrimarySlots, gameObject))
                    {
                        primaryObject.ReleaseSlots(gameObject);
                        target = default;
                        return false;
                    }
                }
            }

            target = new AutonomyTarget(interaction, primaryObject, ambientObject);
            return true;
        }

        private SmartObject TryFindClosestTo(IReadOnlyList<SmartObject> smartObjects, Vector3 referencePosition)
        {
            SmartObject closest = null;
            float minSqrDistance = float.MaxValue;

            foreach (SmartObject item in smartObjects)
            {
                if (item == null)
                {
                    continue;
                }

                float sqrDistance = (item.transform.position - referencePosition).sqrMagnitude;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest = item;
                }
            }

            return closest;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        private float ScoreInteraction(Interaction interaction)
        {
            if (_motiveComponent == null || interaction == null)
            {
                return 0f;
            }

            float totalScore = 0f;

            // Multiply each motive effect by its urgency to prioritize urgent needs
            foreach (var motiveModifier in interaction.MotiveDecayRates)
            {
                float effect = motiveModifier.Value; // Positive = boost, negative = drain
                float urgency = _motiveComponent.GetNormalizedMotiveValue(motiveModifier.Key);

                totalScore += effect * urgency;
            }

            return totalScore;
        }
    }
}
