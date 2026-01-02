using UnityEngine;

namespace SmallAmbitions
{
    public sealed class AutonomyController : MonoBehaviour
    {
        [SerializeField] private SmartObjectRuntimeSet _smartObjects;

        public bool TryGetAutonomyTarget(out SmartObject target)
        {
            target = _smartObjects.GetRandom();

            if (target == null)
            {
                Debug.LogWarning($"{nameof(AutonomyController)}: No SmartObject found.");
                return false;
            }

            return true;
        }
    }
}
