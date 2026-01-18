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
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        // Debug properties - only available in Editor and Development builds
        public string DebugPrimaryPhase => _primaryInteractionRunner != null
            ? GetRunnerPhaseString(_primaryInteractionRunner)
            : "None";

        public string DebugAmbientPhase => _ambientInteractionRunner != null
            ? GetRunnerPhaseString(_ambientInteractionRunner)
            : "None";

        public string DebugPrimaryInteractionName => _primaryInteractionRunner != null && _activePrimaryObject != null
            ? _activePrimaryObject.name
            : "None";

        public string DebugAmbientInteractionName => _activeAmbientObject != null
            ? _activeAmbientObject.name
            : "None";

        private static string GetRunnerPhaseString(InteractionRunner runner)
        {
            if (runner.IsFinished) return "Finished";
            if (runner.IsInExitPhase) return "Exit";
            if (runner.IsLooping) return "Loop";
            if (!runner.HasCompletedStartPhase) return "Start";
            return "Unknown";
        }

#endif

        [Header("References")]
        [SerializeField] private AgentAnimator _animator;
        [SerializeField] private LayerMask _smartObjectLayer;
        [SerializeField] private MotiveComponent _motiveComponent;
        [SerializeField] private NavigationLock _navigationLock;
        [SerializeField] private SerializableMap<InteractionSlotType, IKRig> _interactionSlotBindings;
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;

        private InteractionRunner _ambientInteractionRunner;
        private InteractionRunner _primaryInteractionRunner;

        private SmartObject _activePrimaryObject;
        private SmartObject _activeAmbientObject;

        private bool _ambientPendingCancel;
        private bool _isInterruptRequested;
        private bool _isEndingInteraction;

        public bool IsInteracting { get; private set; }

        private void OnEnable()
        {
            if (_motiveComponent != null)
            {
                _motiveComponent.OnMotiveCritical += HandleMotiveCritical;
            }
        }

        private void OnDisable()
        {
            if (_motiveComponent != null)
            {
                _motiveComponent.OnMotiveCritical -= HandleMotiveCritical;
            }

            StopPrimaryInteraction();
            StopAmbientInteraction();

            IsInteracting = false;
            _isEndingInteraction = false;
        }

        private void LateUpdate()
        {
            bool hasAmbient = _ambientInteractionRunner != null;
            bool hasPrimary = _primaryInteractionRunner != null;

            // Primary runs first when:
            // - There is no ambient OR
            // - Ambient is looping OR
            // - Ambient has completed its Start phase
            bool canRunPrimary = hasPrimary &&
                (!hasAmbient ||
                 _ambientInteractionRunner.IsLooping ||
                 _ambientInteractionRunner.HasCompletedStartPhase);

            // Ambient pauses when:
            // - Primary is still running (not finished) AND
            // - Ambient has completed its Start phase
            // This keeps ambient paused during primary's Exit phase too
            bool shouldPauseAmbient = hasAmbient && hasPrimary &&
                _ambientInteractionRunner.HasCompletedStartPhase &&
                !_primaryInteractionRunner.IsFinished;

            if (canRunPrimary)
            {
                _primaryInteractionRunner.Update();

                if (_primaryInteractionRunner.IsFinished)
                {
                    StopPrimaryInteraction();

                    // Now that primary is done, start ambient's Exit phase if pending
                    if (_ambientPendingCancel && _ambientInteractionRunner != null)
                    {
                        _ambientPendingCancel = false;
                        _ambientInteractionRunner.Cancel();
                    }
                    else
                    {
                        _isInterruptRequested = false;
                        _navigationLock.Unlock();
                        return;
                    }
                }
            }

            if (hasAmbient)
            {
                _ambientInteractionRunner.IsPaused = shouldPauseAmbient;
                _ambientInteractionRunner.Update();

                if (_ambientInteractionRunner.IsFinished)
                {
                    StopAmbientInteraction();
                    _isInterruptRequested = false;
                    _navigationLock.Unlock();
                    return;
                }
            }

            if (_isEndingInteraction && IsInteracting)
            {
                if (_navigationLock == null || !_navigationLock.IsLocked)
                {
                    _isEndingInteraction = false;
                    IsInteracting = false;
                }
            }
        }

        private void HandleMotiveCritical(MotiveType motiveType)
        {
            if (_isInterruptRequested)
            {
                return;
            }

            RequestInterrupt();
        }

        public void RequestInterrupt()
        {
            _isInterruptRequested = true;

            if (_primaryInteractionRunner == null)
            {
                // No primary, just cancel ambient directly
                _ambientInteractionRunner?.Cancel();
                return;
            }

            // Cancel primary immediately
            _primaryInteractionRunner.Cancel();

            // Mark ambient for cancellation after primary finishes
            if (_ambientInteractionRunner != null)
            {
                _ambientPendingCancel = true;
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
            _navigationLock.Lock();
            IsInteracting = true;
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
            IsInteracting = true;
        }

        private void StopAmbientInteraction()
        {
            if (_ambientInteractionRunner == null)
            {
                return;
            }

            _ambientInteractionRunner.ForceCancel();
            _ambientInteractionRunner = null;
            _ambientPendingCancel = false;

            // Important: release ONLY what this layer reserved.
            // Here we assume posture reserved slots on _activeAmbientObject (postureObject).
            _activeAmbientObject?.ReleaseSlots(gameObject);
            _activeAmbientObject = null;

            if (_primaryInteractionRunner == null)
            {
                _isEndingInteraction = true;
            }
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

            if (_ambientInteractionRunner == null)
            {
                _isEndingInteraction = true;
            }
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

        private bool TryFindSmartObjectsInRange(Vector3 origin, float searchRadius, out List<SmartObject> smartObjectsInRange)
        {
            smartObjectsInRange = new List<SmartObject>();

            Collider[] colliders = Physics.OverlapSphere(origin, searchRadius, _smartObjectLayer);

            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent(out SmartObject smartObject))
                {
                    smartObjectsInRange.Add(smartObject);
                }
            }

            return smartObjectsInRange.Count > 0;
        }

        private bool TryFindAvailableAmbientSmartObjects(Interaction interaction, SmartObject primarySmartObject, out List<SmartObject> ambientSmartObjects)
        {
            if (!TryFindSmartObjectsInRange(primarySmartObject.transform.position, interaction.PositionToleranceRadius, out ambientSmartObjects))
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
