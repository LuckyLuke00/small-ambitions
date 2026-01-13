using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class InteractionCandidate
    {
        public Interaction Interaction { get; }
        public SmartObject SmartObject { get; }
        public List<SmartObject> CandidateAmbientSmartObjects { get; }

        public InteractionCandidate(Interaction interaction, SmartObject smartObject)
        {
            Interaction = interaction;
            SmartObject = smartObject;
            CandidateAmbientSmartObjects = new List<SmartObject>();
        }
    }

    public sealed class InteractionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MotiveComponent _motiveComponent;
        [SerializeField] private AgentAnimator _animator;
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;
        [SerializeField] private SerializableMap<InteractionSlotType, IKRig> _interactionSlotBindings;

        private InteractionRunner _ambientInteractionRunner;
        private InteractionRunner _primaryInteractionRunner;

        private SmartObject _activePrimaryObject;
        private SmartObject _activeAmbientObject;

        public bool IsInteracting => _primaryInteractionRunner != null || _ambientInteractionRunner != null;

        private void OnDisable()
        {
            StopPrimaryInteraction();
            StopAmbientInteraction();
        }

        private void LateUpdate()
        {
            bool hasAmbient = _ambientInteractionRunner != null;
            bool hasPrimary = _primaryInteractionRunner != null;

            // Determine if ambient should pause to let primary run
            // Ambient pauses when:
            // - It has completed its Start phase AND
            // - Primary is still running (not finished)
            bool shouldPauseAmbient = hasAmbient && hasPrimary &&
                _ambientInteractionRunner.HasCompletedStartPhase &&
                !_primaryInteractionRunner.IsFinished;

            // Primary can run when:
            // - There is no ambient OR
            // - Ambient is looping OR
            // - Ambient has completed its Start phase (for non-looping ambient)
            bool canRunPrimary = hasPrimary &&
                (!hasAmbient ||
                 _ambientInteractionRunner.IsLooping ||
                 _ambientInteractionRunner.HasCompletedStartPhase);

            if (hasAmbient)
            {
                _ambientInteractionRunner.IsProgressionPaused = shouldPauseAmbient;
                _ambientInteractionRunner.IsAnimationPaused = shouldPauseAmbient;
                _ambientInteractionRunner.Update();

                if (_ambientInteractionRunner.IsFinished)
                {
                    StopAmbientInteraction();
                }
            }

            if (canRunPrimary)
            {
                _primaryInteractionRunner.Update();

                if (_primaryInteractionRunner.IsFinished)
                {
                    StopPrimaryInteraction();
                }
            }
        }

        public bool TryStartInteraction(AutonomyTarget target, bool slotsAlreadyReserved = false)
        {
            var interaction = target.Interaction;
            var primaryObject = target.PrimarySmartObject;
            var ambientObject = target.AmbientSmartObject;

            if (interaction == null || primaryObject == null)
            {
                return false;
            }

            if (_animator == null)
            {
                Debug.LogError($"{nameof(InteractionManager)}: Cannot start interaction, Animator is null.");
                return false;
            }

            bool needsAmbient = interaction.RequiredAmbientSlots != null && interaction.RequiredAmbientSlots.Count > 0;

            if (!slotsAlreadyReserved)
            {
                if (needsAmbient)
                {
                    if (ambientObject == null || ambientObject == primaryObject)
                    {
                        return false;
                    }

                    if (!ambientObject.HasAvailableSlots(interaction.RequiredAmbientSlots))
                    {
                        return false;
                    }
                }

                if (!primaryObject.HasAvailableSlots(interaction.RequiredPrimarySlots))
                {
                    return false;
                }
            }

            StopPrimaryInteraction();

            Interaction required = interaction.RequiredAmbientInteraction;
            if (required != null)
            {
                SmartObject postureObject = ambientObject != null ? ambientObject : primaryObject;
                StartAmbientInteraction(required, postureObject, primaryObject, slotsAlreadyReserved);
            }
            else
            {
                StopAmbientInteraction();
            }

            if (!slotsAlreadyReserved)
            {
                if (!primaryObject.TryReserveSlots(interaction.RequiredPrimarySlots, gameObject))
                {
                    if (needsAmbient)
                    {
                        ambientObject.ReleaseSlots(gameObject);
                    }
                    return false;
                }

                if (needsAmbient && !ambientObject.TryReserveSlots(interaction.RequiredAmbientSlots, gameObject))
                {
                    primaryObject.ReleaseSlots(gameObject);
                    return false;
                }
            }

            _activePrimaryObject = primaryObject;
            _activeAmbientObject = needsAmbient ? ambientObject : null;

            _primaryInteractionRunner = new InteractionRunner(interaction, _animator, primaryObject, _interactionSlotBindings, _activePrimaryObject.InteractionSlots, _motiveComponent);
            return true;
        }

        private void StartAmbientInteraction(Interaction interaction, SmartObject postureObject, SmartObject primaryObject, bool slotsAlreadyReserved = false)
        {
            StopAmbientInteraction();

            if (interaction == null || postureObject == null)
            {
                return;
            }

            if (!slotsAlreadyReserved)
            {
                if (!postureObject.HasAvailableSlots(interaction.RequiredPrimarySlots))
                {
                    return;
                }

                if (!postureObject.TryReserveSlots(interaction.RequiredPrimarySlots, gameObject))
                {
                    return;
                }
            }

            _activeAmbientObject = postureObject;
            _ambientInteractionRunner = new InteractionRunner(interaction, _animator, postureObject, _interactionSlotBindings, _activeAmbientObject.InteractionSlots, _motiveComponent);
        }

        private void StopAmbientInteraction()
        {
            if (_ambientInteractionRunner == null)
            {
                return;
            }

            _ambientInteractionRunner.ForceCancel();
            _ambientInteractionRunner = null;

            // Important: release ONLY what this layer reserved.
            // Here we assume posture reserved slots on _activeAmbientObject (postureObject).
            _activeAmbientObject?.ReleaseSlots(gameObject);
            _activeAmbientObject = null;
        }

        private void StopPrimaryInteraction()
        {
            if (_primaryInteractionRunner == null)
            {
                return;
            }

            _primaryInteractionRunner.ForceCancel();
            _primaryInteractionRunner = null;

            ReleaseSlots(_activePrimaryObject, _activeAmbientObject);

            _activePrimaryObject = null;
            _activeAmbientObject = null;
        }

        private void ReleaseSlots(SmartObject primaryObject, SmartObject ambientObject)
        {
            primaryObject?.ReleaseSlots(gameObject);
            ambientObject?.ReleaseSlots(gameObject);
        }

        public bool TryGetAvailableInteractions(out List<InteractionCandidate> availableInteractions)
        {
            availableInteractions = new List<InteractionCandidate>(_smartObjects.Count);

            foreach (var smartObject in _smartObjects)
            {
                foreach (var interaction in smartObject.Interactions)
                {
                    if (!smartObject.HasAvailableSlots(interaction.RequiredPrimarySlots))
                    {
                        continue;
                    }

                    if (interaction.RequiredAmbientSlots.Count == 0)
                    {
                        availableInteractions.Add(new InteractionCandidate(interaction, smartObject));
                        continue;
                    }

                    if (TryFindAvailableAmbientSmartObjects(interaction, smartObject, out var ambientSmartObjects))
                    {
                        var candidate = new InteractionCandidate(interaction, smartObject);
                        candidate.CandidateAmbientSmartObjects.AddRange(ambientSmartObjects);
                        availableInteractions.Add(candidate);
                    }
                }
            }

            return availableInteractions.Count > 0;
        }

        private bool TryGetSmartObjectsInRangeDistance(float searchRadius, out List<SmartObject> smartObjectsInRange)
        {
            smartObjectsInRange = new List<SmartObject>(_smartObjects.Count);

            Vector3 origin = transform.position;
            float sqrSearchRadius = searchRadius * searchRadius;

            foreach (var smartObject in _smartObjects)
            {
                var sqrDistance = MathUtils.SqrDistance(origin, smartObject.transform.position);

                if (sqrDistance <= sqrSearchRadius)
                {
                    smartObjectsInRange.Add(smartObject);
                }
            }

            return smartObjectsInRange.Count > 0;
        }

        private bool TryFindAvailableAmbientSmartObjects(Interaction interaction, SmartObject primarySmartObject, out List<SmartObject> ambientSmartObjects)
        {
            if (!TryGetSmartObjectsInRangeDistance(interaction.PositionToleranceRadius, out ambientSmartObjects))
            {
                return false;
            }

            ambientSmartObjects.Remove(primarySmartObject);

            for (int i = ambientSmartObjects.Count - 1; i >= 0; --i)
            {
                var smartObject = ambientSmartObjects[i];
                if (!smartObject.HasAvailableSlots(interaction.RequiredAmbientSlots))
                {
                    ambientSmartObjects.RemoveAt(i);
                }
            }

            return ambientSmartObjects.Count > 0;
        }
    }
}
