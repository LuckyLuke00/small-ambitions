using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SmallAmbitions
{
    public sealed class IKController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TwoBoneIKConstraint _rightArmRig;

        private bool _isInteracting = false;
        private IKTarget _currentIKTarget;

        public void Interact(List<IKTarget> ikTargets)
        {
            if (ikTargets == null || ikTargets.Count == 0)
            {
                Debug.LogError($"{nameof(ikTargets)} is invalid.");
                return;
            }
            
            foreach (var target in ikTargets)
            {
                if (!target.IsValid() || target.Type != IKTargetType.IK_RightHand)
                {
                    continue;
                }

                _currentIKTarget = target;
            }
            
            Transform rightArmTarget = _rightArmRig.data.target;
            rightArmTarget.position = _currentIKTarget.Target.position;
            rightArmTarget.rotation = _currentIKTarget.Target.rotation;
            
            _rightArmRig.weight = 1f;

            _isInteracting = true;
        }

        private void Update()
        {
            if (!_isInteracting)
            {
                return;
            }
            
            Transform rightArmTarget = _rightArmRig.data.target;
            rightArmTarget.position = _currentIKTarget.Target.position;
            rightArmTarget.rotation = _currentIKTarget.Target.rotation;
        }
    }
}
