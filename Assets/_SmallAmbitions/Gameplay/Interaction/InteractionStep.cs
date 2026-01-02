using System;
using UnityEngine;

namespace SmallAmbitions
{
    [Serializable]
    public class InteractionStep
    {
        private enum InteractionStepDurationMode
        {
            FixedTime = 0,
            Looping
        }

        [Header("Animation")]
        [SerializeField] private AnimationClip _animationClip = null;
        [SerializeField] private InteractionStepDurationMode _durationMode = InteractionStepDurationMode.FixedTime;
        [SerializeField, Min(0f)] private float _fixedDuration = 1f;

        [Header("Pose")]
        [field: SerializeField, Range(0f, 1f)] public float TargetRigWeight { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float BlendTimeSeconds { get; private set; } = 0.1f;

        public bool IsComplete { get; private set; } = false;
        private float _elapsedTime = 0f;

        private Animation _currentAnimation = null;

        public void Begin(Animation animation)
        {
            IsComplete = false;
            _elapsedTime = 0f;
            _currentAnimation = animation;

            if (!Validate())
            {
                IsComplete = true;
                return;
            }

            PlayAnimation();
            OnBegin();
        }

        public void Update()
        {
            if (IsComplete)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            OnUpdate();

            if (IsStepComplete())
            {
                End();
            }
        }

        public void End()
        {
            if (IsComplete)
            {
                return;
            }

            IsComplete = true;
            OnEnd();
        }

        protected virtual void OnBegin()
        { }

        protected virtual void OnUpdate()
        { }

        protected virtual void OnEnd()
        { }

        private void PlayAnimation()
        {
            if (_animationClip != null && _currentAnimation != null)
            {
                _currentAnimation.Play(_animationClip.name);
            }
        }

        private bool IsStepComplete()
        {
            switch (_durationMode)
            {
                case InteractionStepDurationMode.FixedTime:
                    return _elapsedTime >= _fixedDuration;

                case InteractionStepDurationMode.Looping:
                    return false;

                default:
                    return true;
            }
        }

        private bool Validate()
        {
            if (_durationMode == InteractionStepDurationMode.FixedTime && _fixedDuration <= 0f)
            {
                Debug.LogError($"{nameof(InteractionStep)}: Fixed duration must be greater than zero when duration mode is set to FixedTime.");
                return false;
            }

            if (_currentAnimation == null && _animationClip != null)
            {
                Debug.LogError($"{nameof(InteractionStep)}: Animator must be assigned when an animation clip is specified.");
                return false;
            }

            return true;
        }
    }
}
