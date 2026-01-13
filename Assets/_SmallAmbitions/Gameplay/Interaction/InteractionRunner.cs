using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class InteractionRunner
    {
        private enum Phase
        {
            Start,
            Loop,
            Exit,
            Finished
        }

        private readonly Interaction _interaction;
        private readonly AgentAnimator _animator;
        private readonly SmartObject _smartObject;
        private readonly MotiveComponent _motiveComponent;

        private readonly IReadOnlyDictionary<InteractionSlotType, IKRig> _rigs;
        private readonly IReadOnlyList<InteractionSlotDefinition> _slots;

        private Phase _phase = Phase.Start;
        private int _stepIndex = -1;
        private float _stepTime = 0f;

        private bool _isAnimationPaused;
        private bool _animationSkippedDuringPause;

        public bool IsFinished => _phase == Phase.Finished;
        public bool IsLooping => _phase == Phase.Loop;
        public bool IsInExitPhase => _phase == Phase.Exit;
        public bool HasLoopPhase => _interaction.LoopSteps.Count > 0;
        public bool HasCompletedStartPhase => _phase != Phase.Start;

        /// <summary>
        /// When true, the runner maintains its current state (IK weights, etc.) but does not advance to the next phase.
        /// </summary>
        public bool IsProgressionPaused { get; set; } = false;

        public bool IsAnimationPaused
        {
            get => _isAnimationPaused;
            set
            {
                if (_isAnimationPaused == value)
                {
                    return;
                }

                bool wasUnpaused = _isAnimationPaused && !value;
                _isAnimationPaused = value;

                // Replay animation if we skipped it during pause
                if (wasUnpaused && _animationSkippedDuringPause)
                {
                    _animationSkippedDuringPause = false;
                    var step = GetCurrentStep();
                    ForcePlayAnimation(step);
                }
            }
        }

        public InteractionRunner(Interaction interaction, AgentAnimator animator, SmartObject smartObject, IReadOnlyDictionary<InteractionSlotType, IKRig> rigs, IReadOnlyList<InteractionSlotDefinition> slots, MotiveComponent motiveComponent = null)
        {
            _interaction = interaction;
            _animator = animator;
            _smartObject = smartObject;
            _rigs = rigs;
            _slots = slots;
            _motiveComponent = motiveComponent;

            ApplyMotiveModifiers();
        }

        public void Update()
        {
            if (IsFinished)
            {
                return;
            }

            if (IsStepFinished() && !IsProgressionPaused)
            {
                AdvanceStep();
            }

            var step = GetCurrentStep();
            ApplyIK(step);

            if (step.ResetAttachement)
            {
                _smartObject?.ResetAttachmentObjectTransform();
            }

            if (!IsProgressionPaused)
            {
                _stepTime += Time.deltaTime;
            }
        }

        public void Cancel()
        {
            if (_phase == Phase.Finished || _phase == Phase.Exit)
            {
                return;
            }

            _phase = Phase.Exit;
            _stepIndex = -1;
            _stepTime = 0f;
        }

        public void ForceCancel()
        {
            if (_phase == Phase.Finished)
            {
                return;
            }

            _phase = Phase.Finished;
            Cleanup();
        }

        private void AdvanceStep()
        {
            const int MaxTransitions = 8;

            for (int safety = 0; safety < MaxTransitions; ++safety)
            {
                _stepIndex++;
                _stepTime = 0f;

                var steps = CurrentList();
                if (steps != null && steps.IsValidIndex(_stepIndex))
                {
                    StartStep(steps[_stepIndex]);
                    return;
                }

                switch (_phase)
                {
                    case Phase.Start:
                        _phase = _interaction.LoopSteps.Count > 0 ? Phase.Loop : Phase.Exit;
                        _stepIndex = -1;
                        break;

                    case Phase.Loop:
                        _stepIndex = -1;
                        break;

                    case Phase.Exit:
                        _phase = Phase.Finished;
                        Cleanup();
                        return;
                }
            }

            Debug.LogError("AdvanceStep exceeded max transitions. Invalid interaction configuration?");
            _phase = Phase.Finished;
        }

        private void StartStep(InteractionStep step)
        {
            TryAttachSmartObjectToSlot(step);
            TryPlayAnimation(step);
        }

        private void TryAttachSmartObjectToSlot(InteractionStep step)
        {
            if (step.AttachToSlot == InteractionSlotType.None || _smartObject?.AttachmentObject == null)
            {
                return;
            }

            foreach (var rig in _rigs)
            {
                if (rig.Key == step.AttachToSlot)
                {
                    _smartObject.AttachAttachmentObject(rig.Value.AttachmentPoint);
                    break;
                }
            }
        }

        private void TryPlayAnimation(InteractionStep step)
        {
            if (step.AnimationToPlay == null)
            {
                return;
            }

            if (IsAnimationPaused)
            {
                _animationSkippedDuringPause = true;
                return;
            }

            _animator.PlayOneShot(step.AnimationToPlay, step.AnimationLayer);
        }

        private void ForcePlayAnimation(InteractionStep step)
        {
            if (step.AnimationToPlay != null)
            {
                _animator.PlayOneShot(step.AnimationToPlay, step.AnimationLayer);
            }
        }

        private void ApplyIK(InteractionStep step)
        {
            float targetWeight = Mathf.Clamp01(step.TargetRigWeight);
            float blendSpeed = MathUtils.SafeDivide(Time.deltaTime, step.RigBlendDurationSeconds, fallback: 0f);

            foreach (var rig in _rigs)
            {
                if (!rig.Value.IsActive)
                {
                    continue;
                }

                foreach (var slot in _slots)
                {
                    if (slot.SlotType != rig.Key)
                    {
                        continue;
                    }

                    rig.Value.MoveIKTarget(slot.SlotTransform);
                    rig.Value.Weight = blendSpeed == 0f ? targetWeight : Mathf.MoveTowards(rig.Value.Weight, targetWeight, blendSpeed);
                    break;
                }
            }
        }

        private bool IsStepFinished()
        {
            var step = GetCurrentStep();
            return _stepTime >= step.DurationSeconds;
        }

        private InteractionStep GetCurrentStep()
        {
            var steps = CurrentList();
            if (steps == null || !steps.IsValidIndex(_stepIndex))
            {
                return default;
            }

            return steps[_stepIndex];
        }

        private IReadOnlyList<InteractionStep> CurrentList()
        {
            return _phase switch
            {
                Phase.Start => _interaction.StartSteps,
                Phase.Loop => _interaction.LoopSteps,
                Phase.Exit => _interaction.ExitSteps,
                _ => null
            };
        }

        private void ApplyMotiveModifiers()
        {
            _motiveComponent?.ApplyMotiveModifiers(_interaction.MotiveDecayRates);
        }

        private void RemoveMotiveModifiers()
        {
            _motiveComponent?.RemoveMotiveModifiers(_interaction.MotiveDecayRates);
        }

        private void Cleanup()
        {
            RemoveMotiveModifiers();

            foreach (var rig in _rigs.Values)
            {
                rig.Weight = 0f;
            }

            _smartObject?.ResetAttachmentObjectTransform();
        }
    }
}
