using UnityEngine;
using UnityEngine.AI;

namespace SmallAmbitions
{
    public static class NavMeshAgentExtensions
    {
        public static bool IsReady(this NavMeshAgent agent)
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        /// <summary>Stop movement immediately. No drifting, no pending path.</summary>
        public static void StopImmediately(this NavMeshAgent agent)
        {
            if (!agent.IsReady())
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.nextPosition = agent.transform.position;
        }

        /// <summary>
        /// Resync the agent's internal nav position to the current transform.
        /// Use this after toggling agent enabled state to avoid tiny snaps/sinking.
        /// </summary>
        public static void ResyncToTransform(this NavMeshAgent agent)
        {
            if (!agent.IsReady())
            {
                return;
            }

            agent.Warp(agent.transform.position);
            agent.nextPosition = agent.transform.position;
            agent.velocity = Vector3.zero;
        }

        /// <summary>Clear current path and start moving to a destination.</summary>
        public static bool TryMoveTo(this NavMeshAgent agent, Vector3 destination)
        {
            if (!agent.IsReady())
            {
                return false;
            }

            agent.isStopped = false;
            agent.ResetPath(); // consistent behavior when reusing an agent
            return agent.SetDestination(destination);
        }

        public static bool HasReachedDestination(this NavMeshAgent agent)
        {
            if (!agent.IsReady())
            {
                return false;
            }

            if (agent.pathPending)
            {
                return false;
            }

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }

            // remainingDistance can be Infinity for a frame when path just cleared
            if (float.IsInfinity(agent.remainingDistance))
            {
                return false;
            }

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                return false;
            }

            if (!MathUtils.IsNearlyZero(agent.velocity.sqrMagnitude))
            {
                return false;
            }

            return true;
        }
    }
}
