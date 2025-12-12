using UnityEngine;
using UnityEngine.AI;

namespace SmallAmbitions
{
    public sealed class AgentBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent _agent_TEMP;
        [SerializeField] private SmartObjectSet _smartObjectSet;
        [SerializeField] private IKController  _ikController;

        private SmartObject _currentFocus;

        private float secondsToWait = 5f;
        private float elapsedSeconds = 0f;

        private bool started = false;
        private bool started2 = false;

        private void Update()
        {
            secondsToWait += Time.deltaTime;
            if (!started && secondsToWait >= elapsedSeconds)
            {
                started = true;
                GoFindCoffee();
            }

            if (!started2 && started && _currentFocus && _agent_TEMP.destination == transform.position)
            {
                Debug.Log("Coffee found");
                _ikController.Interact(_currentFocus.IKTargets);
                started2 = true;
            }
        }

        private void GoFindCoffee()
        {
            _currentFocus = _smartObjectSet.FindClosest(transform.position);
            if (_currentFocus == null)
            {
                Debug.Log("Coffee not found");
            }

            if (_agent_TEMP == null)
            {
                Debug.Log("Coffee not found");
            }

            _agent_TEMP.SetDestination(_currentFocus.StandingSpot.position);
        }
    }
}