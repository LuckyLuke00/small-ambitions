using System;
using System.Collections;
using UnityEngine;

namespace SmallAmbitions
{
    public class InteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private IKController _ikController;

        public event Action OnInteractionFinished;

        private Coroutine _interactionRoutine;

        public void StartInteraction(SmartObject smartObject)
        {
            this.SafeStartCoroutine(ref _interactionRoutine, InteractionSequence(smartObject));
        }

        public void StopInteraction(SmartObject smartObject)
        {
            this.SafeStopCoroutine(ref _interactionRoutine);

            foreach (var target in smartObject.IKTargets)
            {
                _ikController.RevertIKTarget(target);
            }

            _interactionRoutine = null;
        }

        private IEnumerator InteractionSequence(SmartObject smartObject)
        {
            SnapToTarget(smartObject.StandingSpot);

            foreach (var target in smartObject.IKTargets)
            {
                _ikController.ApplyIKTarget(target);
            }

            Debug.Log($"[Interaction] Started {smartObject.name}. Waiting {smartObject.InteractionTime}s...");

            float timer = 0f;
            while (timer < smartObject.InteractionTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[Interaction] Finished {smartObject.name}.");
            foreach (var target in smartObject.IKTargets)
            {
                _ikController.RevertIKTarget(target);
            }

            OnInteractionFinished?.Invoke();
        }

        private void SnapToTarget(Transform targetTransform)
        {
            Vector3 targetPosition = targetTransform.position;
            targetPosition.y = transform.position.y;

            transform.position = targetPosition;
            transform.rotation = targetTransform.rotation;
        }
    }
}
