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
        [SerializeField] private AgentAnimator _animator;
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;
        [SerializeField] private SerializableMap<InteractionSlotType, IKRig> _interactionSlotBindings;

        private Interaction _activeInteraction;
        private InteractionRunner _activeRunner;

        private SmartObject _activePrimary;
        private SmartObject _activeAmbient;

        public bool IsInteracting => _activeRunner != null;

        private void Update()
        {
            if (_activeRunner == null)
            {
                return;
            }

            _activeRunner.Update(Time.deltaTime);

            if (_activeRunner.IsInteractionComplete)
            {
                StopInteraction();
            }
        }

        public void StopInteraction()
        {
            _activeRunner = null;

            if (_activePrimary != null)
            {
                _activePrimary.ReleaseSlots(gameObject);
                _activePrimary = null;
            }

            if (_activeAmbient != null)
            {
                _activeAmbient.ReleaseSlots(gameObject);
                _activeAmbient = null;
            }

            _activeInteraction = null;
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
                        var interactionCandidate = new InteractionCandidate(interaction, smartObject);
                        interactionCandidate.CandidateAmbientSmartObjects.AddRange(ambientSmartObjects);
                        availableInteractions.Add(interactionCandidate);
                    }
                }
            }

            return availableInteractions.Count > 0;
        }

        public bool TryStartInteraction(Interaction interaction, SmartObject primarySmartObject, SmartObject ambientSmartObject = null)
        {
            if (interaction == null || primarySmartObject == null)
            {
                return false;
            }

            if (_animator == null)
            {
                Debug.LogError($"{nameof(InteractionManager)}: Cannot start interaction, Animator is null.");
                return false;
            }

            if (_activeRunner != null)
            {
                StopInteraction();
            }

            bool needsAmbient = interaction.RequiredAmbientSlots != null && interaction.RequiredAmbientSlots.Count > 0;
            if (needsAmbient)
            {
                if (ambientSmartObject == null || ambientSmartObject == primarySmartObject || !ambientSmartObject.TryReserveSlots(interaction.RequiredAmbientSlots, gameObject))
                {
                    return false;
                }
            }

            if (!primarySmartObject.TryReserveSlots(interaction.RequiredPrimarySlots, gameObject))
            {
                return false;
            }

            _activeInteraction = interaction;
            _activePrimary = primarySmartObject;
            _activeAmbient = needsAmbient ? ambientSmartObject : null;

            _activeRunner = new InteractionRunner(interaction, _animator, primarySmartObject);
            _activeRunner.Initialize(_interactionSlotBindings, primarySmartObject.InteractionSlots);

            return true;
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

        private bool TryGetSmartObjectsInRangeOverlap(float searchRadius, out List<SmartObject> smartObjectsInRange)
        {
            Vector3 origin = transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, searchRadius);
            smartObjectsInRange = new List<SmartObject>(hits.Length);

            foreach (var hit in hits)
            {
                SmartObject smartObject = hit.GetComponentInParent<SmartObject>();
                if (smartObject != null)
                {
                    smartObjectsInRange.Add(smartObject);
                }
            }

            return smartObjectsInRange.Count > 0;
        }

        private bool TryFindAvailableAmbientSmartObjects(Interaction interaction, SmartObject primarySmartObject, out List<SmartObject> ambientSmartObjects)
        {
            if (!TryGetSmartObjectsInRangeOverlap(interaction.PositionToleranceRadius, out ambientSmartObjects))
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
