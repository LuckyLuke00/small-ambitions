using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    [System.Serializable]
    public sealed class InteractionSequence
    {
        [SerializeField] private List<InteractionStep> _steps = new();

        private Animation _currentAnimation = null;
        private IReadOnlyDictionary<InteractionSlotType, IKRig> _interactionSlotBindings;
        private IReadOnlyList<InteractionSlotDefinition> _slotTargets;

        private InteractionStep _currentStep = null;
        private int _currentStepIndex = -1;

        public bool IsComplete { get; private set; } = false;

        public void Begin(Animation animation, IReadOnlyDictionary<InteractionSlotType, IKRig> rigBindings, IReadOnlyList<InteractionSlotDefinition> slotTargets)
        {
            if (_steps.Count == 0 || animation == null)
            {
                Debug.LogWarning($"{nameof(InteractionSequence)}: Cannot begin sequence. No steps defined or animator is null.");
                IsComplete = true;
                return;
            }

            IsComplete = false;

            _currentAnimation = animation;
            _currentStepIndex = 0;

            _interactionSlotBindings = rigBindings;
            _slotTargets = slotTargets;

            _currentStep = _steps[_currentStepIndex];
            _currentStep.Begin(animation);
        }

        public void Update()
        {
            if (_currentStep == null)
            {
                IsComplete = true;
                return;
            }

            _currentStep.Update();
            BlendInIkWeights();

            if (_currentStep.IsComplete)
            {
                AdvanceStep();
            }
        }

        public void End()
        {
            _currentStep?.End();
            _currentStep = null;

            ResetIkWeights();
            _interactionSlotBindings = null;

            _currentAnimation = null;

            IsComplete = true;
        }

        private void AdvanceStep()
        {
            ++_currentStepIndex;
            if (!Utils.IsValidIndex(_steps, _currentStepIndex))
            {
                End();
                return;
            }
            _currentStep = _steps[_currentStepIndex];
            _currentStep.Begin(_currentAnimation);
        }

        private void BlendInIkWeights()
        {
            if (_currentStep == null || _interactionSlotBindings == null || _slotTargets == null)
            {
                return;
            }

            float targetWeight = Mathf.Clamp01(_currentStep.TargetRigWeight);
            float blendTime = MathUtils.SafeDivide(Time.deltaTime, _currentStep.BlendTimeSeconds, fallback: 0f);

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
                }

                ikRig.Weight = Mathf.MoveTowards(ikRig.Weight, targetWeight, blendTime);
            }
        }

        private void ResetIkWeights()
        {
            if (_interactionSlotBindings == null)
            {
                return;
            }

            foreach (var rig in _interactionSlotBindings.Values)
            {
                rig.Weight = 0f;
            }
        }
    }
}
