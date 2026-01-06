using System.Collections.Generic;
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
        public AutonomyTarget CurrentAutonomyTarget { get; private set; } // Need to cache it, because Unity Behaviors can't hold references to structs

        public bool AquireNewAutonomyTarget()
        {
            if (!_interactionManager.TryGetAvailableInteractions(out List<InteractionCandidate> candidates))
            {
                Debug.LogWarning($"{nameof(AutonomyController)}: No Interaction Candidates found.");
                return false;
            }

            CurrentAutonomyTarget = ChooseBestCandidate(candidates);
            return true;
        }

        private AutonomyTarget ChooseBestCandidate(IReadOnlyList<InteractionCandidate> candidates)
        {
            // Placeholder: Random selection for now
            InteractionCandidate bestCandidate = candidates.GetRandomElement();
            SmartObject closestAmbientSmartObject = TryFindClosest(bestCandidate.CandidateAmbientSmartObjects);
            return new AutonomyTarget(bestCandidate.Interaction, bestCandidate.SmartObject, closestAmbientSmartObject);
        }

        private SmartObject TryFindClosest(IReadOnlyList<SmartObject> smartObjects)
        {
            SmartObject closest = null;
            float minSqrDistance = float.MaxValue;

            foreach (SmartObject item in smartObjects)
            {
                if (item == null)
                {
                    continue;
                }

                float sqrDistance = (item.transform.position - transform.position).sqrMagnitude;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest = item;
                }
            }

            return closest;
        }
    }
}
