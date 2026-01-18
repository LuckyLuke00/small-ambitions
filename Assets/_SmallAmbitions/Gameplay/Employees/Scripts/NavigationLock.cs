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

        private int _lockCount;
        public bool IsLocked => _lockCount > 0;

        private void OnEnable()
        {
            _lockCount = 0;
            RemoveLock();
        }

        private void OnDisable()
        {
            _lockCount = 0;
            RemoveLock();
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
                RemoveLock();
            }
        }

        private void ApplyLock()
        {
            if (_agent != null)
            {
                if (_agent.enabled && _agent.isOnNavMesh)
                {
                    _agent.StopImmediately();
                }

                _agent.enabled = false;
            }

            if (_enableObstacleWhenLocked && _obstacle != null)
            {
                _obstacle.enabled = true;
            }
        }

        private void RemoveLock()
        {
            if (_obstacle != null)
            {
                _obstacle.enabled = false;
            }

            if (_agent == null)
            {
                return;
            }

            if (!_agent.enabled)
            {
                _agent.enabled = true;
            }

            if (_agent.isOnNavMesh)
            {
                _agent.ResyncToTransform();
                _agent.isStopped = false;
            }
        }
    }
}
