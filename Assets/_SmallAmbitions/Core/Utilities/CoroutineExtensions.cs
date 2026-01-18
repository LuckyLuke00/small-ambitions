using System.Collections;
using UnityEngine;

namespace SmallAmbitions
{
    public static class CoroutineExtensions
    {
        public static void SafeStartCoroutine(this MonoBehaviour host, ref Coroutine routine, IEnumerator routineMethod)
        {
            // 1. Safety: If object is dead, don't try to run code
            if (host == null || !host.isActiveAndEnabled)
            {
                return;
            }

            // 2. Cleanup: Stop existing
            if (routine != null)
            {
                host.StopCoroutine(routine);
                routine = null;
            }

            // 3. Execution: Start new & assign
            routine = host.StartCoroutine(routineMethod);
        }

        public static void SafeStopCoroutine(this MonoBehaviour host, ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            // We don't check isActiveAndEnabled here because StopCoroutine
            // is safe to call on disabled objects (it just does nothing)
            if (host)
            {
                host.StopCoroutine(routine);
            }

            routine = null;
        }
    }
}
