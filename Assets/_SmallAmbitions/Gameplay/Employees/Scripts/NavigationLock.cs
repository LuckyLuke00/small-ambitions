using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SmallAmbitions
{
    public sealed class NavigationLock : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private NavMeshObstacle _obstacle;

        [Header("Settings")]
        [SerializeField] private bool _enableObstacleWhenLocked = true;

        public bool IsLocked => _lockCount > 0 || _unlockRoutine != null;

        private int _lockCount = 0;
        private Coroutine _unlockRoutine;

        private void OnEnable()
        {
            _lockCount = 0;
            this.SafeStopCoroutine(ref _unlockRoutine);
            SetObstacleEnabled(false);
            EnableAndResyncAgent();
        }

        private void OnDisable()
        {
            _lockCount = 0;
            this.SafeStopCoroutine(ref _unlockRoutine);
            SetObstacleEnabled(false);
        }

        public void Lock()
        {
            ++_lockCount;
            if (_lockCount == 1)
            {
                ApplyLock();
            }
        }

        public void Unlock()
        {
            if (_lockCount <= 0)
            {
                _lockCount = 0;
                return;
            }

            --_lockCount;
            if (_lockCount == 0)
            {
                BeginUnlock();
            }
        }

        private void ApplyLock()
        {
            this.SafeStopCoroutine(ref _unlockRoutine);

            _agent.StopImmediately();

            SetAgentEnabled(false);

            if (_enableObstacleWhenLocked)
            {
                SetObstacleEnabled(true);
            }
        }

        private void BeginUnlock()
        {
            SetObstacleEnabled(false);
            this.SafeStartCoroutine(ref _unlockRoutine, UnlockNextFrame());
        }

        private IEnumerator UnlockNextFrame()
        {
            // Wait 1 frame: gives Unity a chance to process the obstacle toggle and schedule carving changes.
            yield return null;

            // Wait 2nd frame: ensures the carved hole removal is actually reflected for navmesh queries.
            // Without this, enabling the agent can still see stale data and snap/teleport to the nearest valid point.
            yield return null;

            EnableAndResyncAgent();
            _unlockRoutine = null;
        }

        private void EnableAndResyncAgent()
        {
            SetObstacleEnabled(false);
            SetAgentEnabled(true);
            _agent.ResyncToTransform();
            _agent.isStopped = false;
        }

        private void SetAgentEnabled(bool enabled = true)
        {
            if (_agent != null && _agent.enabled != enabled)
            {
                _agent.enabled = enabled;
            }
        }

        private void SetObstacleEnabled(bool enabled = true)
        {
            if (_obstacle != null && _obstacle.enabled != enabled)
            {
                _obstacle.enabled = enabled;
            }
        }
    }
}
