using System.Collections;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class IKController : MonoBehaviour
    {
        [SerializeField] private SerializableMap<IKTargetType, IKRig> _rigs;

        public void ApplyIKTarget(IKTarget target)
        {
            if (!target.IsValid() || !_rigs.TryGetValue(target.Type, out IKRig rig))
                return;

            rig.MoveIKTarget(target.Transform);

            BlendRigWeight(rig, false);
        }

        public void RevertIKTarget(IKTarget target)
        {
            if (!target.IsValid() || !_rigs.TryGetValue(target.Type, out IKRig rig))
                return;

            BlendRigWeight(rig, true);
        }

        private void BlendRigWeight(IKRig rig, bool blendOut = false)
        {
            float targetWeight = blendOut ? rig.DefaultWeight : rig.TargetWeight;
            float targetSpeed = blendOut ? rig.BlendInSpeed : rig.BlendOutSpeed;

            if (!isActiveAndEnabled)
            {
                rig.Weight = targetWeight;
                return;
            }

            IEnumerator blendRigWeightCoroutine = BlendRigWeightCoroutine(rig, targetWeight, targetSpeed);
            this.SafeStartCoroutine(ref rig.ActiveRoutine, blendRigWeightCoroutine);
        }

        private IEnumerator BlendRigWeightCoroutine(IKRig rig, float targetWeight, float speed)
        {
            while (!Mathf.Approximately(rig.Weight, targetWeight))
            {
                rig.Weight = Mathf.MoveTowards(rig.Weight, targetWeight, speed * Time.deltaTime);
                yield return null;
            }

            rig.Weight = targetWeight;
        }
    }
}
