using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find Random NavMesh Position", story: "Find Random NavMesh [Position] for [Agent]", category: "Action", id: "33638a73a2ba172e622b6877ad2f3f8c")]
public partial class FindRandomNavMeshPositionAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> Position;
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> SearchRadius = new BlackboardVariable<float>(10.0f);

    protected override Status OnStart()
    {
        if (Agent.Value == null)
        {
            Debug.LogWarning("Agent is null.");
            return Status.Failure;
        }

        if (SearchRadius.Value <= 0f)
        {
            Debug.LogWarning("SearchRadius must be greater than zero.");
            return Status.Failure;
        }

        if (!TryFindRandomNavMeshPosition(Agent.Value.transform.position, SearchRadius.Value, out Vector3 foundPosition))
        {
            return Status.Failure;
        }

        Position.Value = foundPosition;
        return Status.Success;
    }

    private bool TryFindRandomNavMeshPosition(Vector3 origin, float radius, out Vector3 result)
    {
        Vector3 randomPosition = origin + UnityEngine.Random.insideUnitSphere * radius;

        if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }
}
