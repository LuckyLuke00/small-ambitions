using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Try Move to Autonomy Target", story: "[Agent] moves to [AutonomyController]'s autonomy target", category: "Action", id: "80c12a24784f85827927bfd541e660ab")]
public partial class TryMoveToAutonomyTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<AutonomyController> AutonomyController;

    private NavMeshAgent _agent;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;

    protected override Status OnStart()
    {
        if (Agent.Value == null || AutonomyController == null)
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
            if (IsFacingSmartObject())
            {
                return Status.Success;
            }

            RotateTowardsSmartObject();
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

        var target = AutonomyController.Value.CurrentAutonomyTarget;
        if (!target.PrimarySmartObject.TryGetAvailableStandPosition(out Transform StandPosition))
        {
            if (!target.AmbientSmartObject.TryGetAvailableStandPosition(out StandPosition))
            {
                return Status.Failure;
            }
        }

        _targetPosition = StandPosition.position;
        _targetRotation = StandPosition.rotation;

        if (!_agent.SetDestination(_targetPosition))
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    private void RotateTowardsSmartObject()
    {
        Transform agentTransform = _agent.transform;
        agentTransform.rotation = Quaternion.RotateTowards(agentTransform.rotation, _targetRotation,
            _agent.angularSpeed * Time.deltaTime);
    }

    private bool IsFacingSmartObject()
    {
        return _agent.transform.rotation == _targetRotation;
    }
}
