using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<InteractionSlotType, InteractionSlotDefinition> _slotsByType;

        private Phase _phase = Phase.Start;
        private int _stepIndex = -1;
        private float _stepTime = 0f;

        public bool IsFinished => _phase == Phase.Finished;
        public bool IsLooping => _phase == Phase.Loop;
        public bool IsInExitPhase => _phase == Phase.Exit;
        public bool HasLoopPhase => _interaction.LoopSteps.Count > 0;
        public bool HasCompletedStartPhase => _phase != Phase.Start;

        /// <summary>
        /// When true, the runner maintains its current state (IK weights, etc.) but does not advance to the next step or play new animations.
        /// </summary>
        public bool IsPaused { get; set; } = false;

        public InteractionRunner(Interaction interaction, AgentAnimator animator, SmartObject smartObject, IReadOnlyDictionary<InteractionSlotType, IKRig> rigs, IReadOnlyList<InteractionSlotDefinition> slots, MotiveComponent motiveComponent = null)
        {
            _interaction = interaction;
            _animator = animator;
            _smartObject = smartObject;
            _rigs = rigs;
            _slots = slots;
            _motiveComponent = motiveComponent;

            _slotsByType = slots.ToDictionary(slot => slot.SlotType, slot => slot);

            ApplyMotiveModifiers();
        }

        public void Update()
        {
            if (IsFinished)
            {
                return;
            }

            if (!IsPaused && IsStepFinished())
            {
                AdvanceStep();
            }

            var step = GetCurrentStep();

            // Always update IK, even when paused
            ApplyIK(step);

            if (step.ResetAttachment)
            {
                _smartObject?.ResetAttachmentObjectTransform();
            }

            if (!IsPaused)
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

            // Immediately advance to start the first exit step (or finish if no exit steps)
            AdvanceStep();
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

            if (_rigs.TryGetValue(step.AttachToSlot, out var rig))
            {
                _smartObject.AttachAttachmentObject(rig.AttachmentPoint);
            }
        }

        private void TryPlayAnimation(InteractionStep step)
        {
            if (step.AnimationToPlay == null)
            {
                return;
            }

            _animator.PlayOneShot(step.AnimationToPlay, step.AnimationLayer);
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

                if (!_slotsByType.TryGetValue(rig.Key, out var slot))
                {
                    continue;
                }

                rig.Value.MoveIKTarget(slot.SlotTransform);
                rig.Value.Weight = MathUtils.IsNearlyZero(blendSpeed) ? targetWeight : Mathf.MoveTowards(rig.Value.Weight, targetWeight, blendSpeed);
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

            _animator.StopAllOneShots();
            _smartObject?.ResetAttachmentObjectTransform();
        }
    }
}
