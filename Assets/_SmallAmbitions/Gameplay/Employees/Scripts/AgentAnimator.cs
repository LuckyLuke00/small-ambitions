using UnityEngine;
using UnityEngine.AI;

namespace SmallAmbitions
{
    public enum OneShotAnimationLayer
    {
        Base,
        UpperBody
    }

    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public sealed class AgentAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private NavMeshAgent _navMeshAgent;

        [Header("One-Shot Animation")]
        [SerializeField] private AnimationClip _oneShotPlaceholder;
        [SerializeField] private SerializableMap<OneShotAnimationLayer, AnimatorParameter> _oneShotTriggers;

        [Header("Locomotion")]
        [SerializeField] private AnimatorParameter _speed;

        private AnimatorOverrideController _oneShotOverride;

        private void Awake()
        {
            _oneShotOverride = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _oneShotOverride;
        }

        private void Update()
        {
            float speedPercent = MathUtils.SafeDivide(_navMeshAgent.velocity.magnitude, _navMeshAgent.speed);
            _animator.SetFloat(_speed.Hash, speedPercent);
        }

        public void PlayOneShot(AnimationClip clip, OneShotAnimationLayer layer = OneShotAnimationLayer.Base)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[{nameof(AgentAnimator)}] Cannot play one-shot animation: clip is null.");
                return;
            }

            if (!_oneShotTriggers.TryGetValue(layer, out AnimatorParameter oneShotTrigger))
            {
                Debug.LogWarning($"[{nameof(AgentAnimator)}] Cannot play one-shot animation: no one-shot trigger configured for layer '{layer}'.");
                return;
            }

            _animator.ResetTrigger(oneShotTrigger.Hash);
            _animator.SetTrigger(oneShotTrigger.Hash);
        }
    }
}
