using UnityEngine;
using UnityEngine.AI;

namespace SmallAmbitions
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class AgentAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent _navMeshAgent;
        [SerializeField] private Animator _animator;

        [Header("Animator Parameters")]
        [SerializeField] private AnimatorParameter _speed;

        private void Update()
        {
            float speedPercent = _navMeshAgent.velocity.sqrMagnitude / (_navMeshAgent.speed * _navMeshAgent.speed);
        }
    }
}
