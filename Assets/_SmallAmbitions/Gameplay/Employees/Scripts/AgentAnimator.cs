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
        [SerializeField] private SerializableMap<OneShotAnimationLayer, AnimatorParameter> _oneShotBools;

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

            if (!_oneShotTriggers.TryGetValue(layer, out AnimatorParameter trigger) ||
                !_oneShotBools.TryGetValue(layer, out AnimatorParameter stayBool))
            {
                Debug.LogWarning($"[{nameof(AgentAnimator)}] No one-shot parameters configured for layer '{layer}'.");
                return;
            }

            _oneShotOverride[_oneShotPlaceholder.name] = clip;
            _animator.ResetTrigger(trigger.Hash);
            _animator.SetBool(stayBool.Hash, true);
            _animator.SetTrigger(trigger.Hash);
        }

        public void StopAllOneShots()
        {
            foreach (var pair in _oneShotTriggers)
            {
                _animator.ResetTrigger(pair.Value.Hash);
            }

            foreach (var pair in _oneShotBools)
            {
                _animator.SetBool(pair.Value.Hash, false);
            }
        }
    }
}
