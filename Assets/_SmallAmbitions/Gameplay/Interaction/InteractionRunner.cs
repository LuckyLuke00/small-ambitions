using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class InteractionRunner
    {
        private readonly Animator _animator;
        private readonly Interaction _interaction;
        private readonly SmartObject _primarySmartObject;

        private IReadOnlyDictionary<InteractionSlotType, IKRig> _interactionSlotBindings;
        private IReadOnlyList<InteractionSlotDefinition> _slotTargets;

        private int _stepIndex = -1;
        private float _elapsedSeconds = 0f;

        public bool IsInteractionComplete { get; private set; } = false;

        public InteractionRunner(Interaction interaction, Animator animator = null, SmartObject primarySmartObject = null)
        {
            _interaction = interaction;
            _animator = animator;
            _primarySmartObject = primarySmartObject;

            _stepIndex = -1;
            _elapsedSeconds = 0f;
            IsInteractionComplete = false;
        }

        public void Initialize(IReadOnlyDictionary<InteractionSlotType, IKRig> interactionSlotBindings, IReadOnlyList<InteractionSlotDefinition> slotTargets)
        {
            _interactionSlotBindings = interactionSlotBindings;
            _slotTargets = slotTargets;
        }

        public void Update(float deltaTime)
        {
            if (IsInteractionComplete)
            {
                return;
            }

            if (IsCurrentStepComplete())
            {
                AdvanceStep();

                if (IsInteractionComplete)
                {
                    return;
                }
            }

            var currentStep = _interaction.Steps[_stepIndex];

            ApplyIKWeight(currentStep);
            AttachToSlot(currentStep);
            PlayAnimation(currentStep);

            if (currentStep.ResetAttachement)
            {
                _primarySmartObject?.ResetAttachmentObjectTransform();
            }

            _elapsedSeconds += deltaTime;
        }

        private bool IsCurrentStepComplete()
        {
            if (!Utils.IsValidIndex(_interaction.Steps, _stepIndex))
            {
                return true;
            }

            var step = _interaction.Steps[_stepIndex];
            return _elapsedSeconds >= step.DurationSeconds;
        }

        private void AdvanceStep()
        {
            ++_stepIndex;
            _elapsedSeconds = 0f;

            if (!Utils.IsValidIndex(_interaction.Steps, _stepIndex))
            {
                IsInteractionComplete = true;
                return;
            }
        }

        private void ApplyIKWeight(InteractionStep interactionStep)
        {
            float targetRigWeight = Mathf.Clamp01(interactionStep.TargetRigWeight);
            float rigWeightBlendTime = MathUtils.SafeDivide(Time.deltaTime, interactionStep.DurationSeconds, fallback: 0f);

            foreach (var pair in _interactionSlotBindings)
            {
                var slotType = pair.Key;
                var ikRig = pair.Value;

                foreach (var slotDef in _slotTargets)
                {
                    if (slotDef.SlotType != slotType)
                    {
                        continue;
                    }

                    ikRig.MoveIKTarget(slotDef.SlotTransform);
                    ikRig.Weight = Mathf.MoveTowards(ikRig.Weight, targetRigWeight, rigWeightBlendTime);
                    break;
                }
            }
        }

        private void AttachToSlot(InteractionStep interactionStep)
        {
            if (interactionStep.AttachToSlot == InteractionSlotType.None)
            {
                return;
            }

            if (_primarySmartObject == null || _primarySmartObject.AttachmentObject == null)
            {
                return;
            }

            foreach (var pair in _interactionSlotBindings)
            {
                var slotType = pair.Key;
                Transform attachementPoint = pair.Value.AttachmentPoint;

                // Parent the attachment object to the rig's transform if the slot types match
                if (slotType == interactionStep.AttachToSlot)
                {
                    _primarySmartObject.AttachAttachmentObject(attachementPoint);
                    break;
                }
            }
        }

        private void PlayAnimation(InteractionStep interactionStep)
        {
            var animation = interactionStep.AnimationToPlay;
            if (animation == null)
            {
                return;
            }

            _animator.Play(animation.name);
        }
    }
}
