using System.Linq;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class Utils : MonoBehaviour
    {
        public static bool HasAnimatorParameter(Animator animator, string parameterName)
        {
            if (animator == null)
            {
                Debug.LogWarning("Animator reference is null.");
                return false;
            }

            int parameterHash = Animator.StringToHash(parameterName);
            return animator.parameters.Any(p => p.nameHash == parameterHash);
        }

        public static bool HasAnimatorParameter(Animator animator, int parameterHash)
        {
            if (animator == null)
            {
                Debug.LogWarning("Animator reference is null.");
                return false;
            }

            return animator.parameters.Any(p => p.nameHash == parameterHash);
        }
    }
}
