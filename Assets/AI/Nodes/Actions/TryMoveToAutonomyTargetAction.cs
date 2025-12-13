using System;
using SmallAmbitions;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TryMoveToAutonomyTarget", story: "[Agent] moves to [AutonomyTarget]", category: "Action",
    id: "15a00288a93f108e60cc670ab9ce36a7")]
public partial class TryMoveToAutonomyTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<SmartObject> AutonomyTarget;

    private NavMeshAgent _agent;
    private Vector3 _targetPosition;

    protected override Status OnStart()
    {
        if (Agent.Value == null || AutonomyTarget.Value == null)
        {
            return Status.Failure;
        }

        return Initialize();
    }

    // NOTE: Might fail when we add doors later
    protected override Status OnUpdate()
    {
        if (_agent.pathPending)
        {
            return Status.Running;
        }

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return Status.Failure;
        }

        if (_agent.remainingDistance <= _agent.stoppingDistance)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_agent == null)
        {
            return;
        }

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }

        _agent = null;
    }

    private Status Initialize()
    {
        _agent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (_agent == null)
        {
            return Status.Failure;
        }

        if (!_agent.isOnNavMesh)
        {
            return Status.Failure;
        }
        
        _agent.ResetPath();
        _targetPosition = AutonomyTarget.Value.StandingSpot.position;

        if (!_agent.SetDestination(_targetPosition))
        {
            return Status.Failure;
        }

        return Status.Running;
    }
}