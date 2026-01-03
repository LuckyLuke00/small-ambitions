using UnityEngine;
using UnityEngine.AI;

namespace SmallAmbitions
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public sealed class AgentAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private NavMeshAgent _navMeshAgent;

        [Header("One-Shot Animation")]
        [SerializeField] private AnimationClip _oneShotPlaceholder;
        [SerializeField] private AnimatorParameter _oneShotTrigger;

        [Header("Locomotion")]
        [SerializeField] private AnimatorParameter _speed;

        private AnimatorOverrideController _oneShotOverride;

        private void Update()
        {
            float speedPercent = MathUtils.SafeDivide(_navMeshAgent.velocity.magnitude, _navMeshAgent.speed);
            _animator.SetFloat(_speed.Hash, speedPercent);
        }

        public void PlayOneShot(AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[{nameof(AgentAnimator)}] Cannot play one-shot animation: clip is null.");
                return;
            }

            if (_oneShotOverride == null)
            {
                _oneShotOverride = new AnimatorOverrideController(_animator.runtimeAnimatorController);
                _animator.runtimeAnimatorController = _oneShotOverride;
            }

            _oneShotOverride[_oneShotPlaceholder] = clip;
            _animator.ResetTrigger(_oneShotTrigger.Hash);
            _animator.SetTrigger(_oneShotTrigger.Hash);
        }
    }
}
