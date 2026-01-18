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
        if (Agent.Value == null || AutonomyController.Value == null)
        {
            return Status.Failure;
        }

        return Initialize();
    }

    // TODO: Handle NavMesh/door interactions for autonomy targets when doors are added
    protected override Status OnUpdate()
    {
        if (_agent == null || !_agent.IsReady())
        {
            return Status.Failure;
        }

        if (!_agent.HasReachedDestination())
        {
            return Status.Running;
        }

        _agent.StopImmediately();

        if (IsFacingTarget())
        {
            return Status.Success;
        }

        RotateTowardsTarget();
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _agent?.StopImmediately();

        if (CurrentStatus == Status.Failure && AutonomyController.Value != null)
        {
            AutonomyController.Value.ReleaseCurrentTarget();
        }

        _agent = null;
    }

    private Status Initialize()
    {
        _agent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (!_agent.IsReady())
        {
            Debug.LogError("TryMoveToAutonomyTargetAction: Agent is not ready (disabled or not on NavMesh).");
            return Status.Failure;
        }

        var target = AutonomyController.Value.CurrentAutonomyTarget;

        if (target.PrimarySmartObject == null)
        {
            Debug.LogError("TryMoveToAutonomyTargetAction: PrimarySmartObject is null. Did AcquireAutonomyTarget succeed?");
            return Status.Failure;
        }

        if (!TryGetStandPosition(target, Agent.Value, out _targetPosition, out _targetRotation))
        {
            Debug.LogError($"TryMoveToAutonomyTargetAction: No stand position reserved for agent {Agent.Value.name}. Primary: {target.PrimarySmartObject?.name}, Ambient: {target.AmbientSmartObject?.name}");
            return Status.Failure;
        }

        if (!_agent.TryMoveTo(_targetPosition))
        {
            Debug.LogError("TryMoveToAutonomyTargetAction: Failed to set NavMesh destination.");
            return Status.Failure;
        }

        return Status.Running;
    }

    private static bool TryGetStandPosition(AutonomyTarget target, GameObject agent, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = default;

        Transform standPosition = default;

        // First try to get the stand position from the primary smart object
        if (target.PrimarySmartObject != null && target.PrimarySmartObject.TryGetStandPositionForUser(agent, out standPosition) && standPosition != null)
        {
            position = standPosition.position;
            rotation = standPosition.rotation;
            return true;
        }

        // Then try to get the stand position from the ambient smart object
        if (target.AmbientSmartObject != null && target.AmbientSmartObject.TryGetStandPositionForUser(agent, out standPosition) && standPosition != null)
        {
            position = standPosition.position;
            rotation = standPosition.rotation;
            return true;
        }

        return false;
    }

    private void RotateTowardsTarget()
    {
        Transform agentTransform = _agent.transform;
        agentTransform.rotation = Quaternion.RotateTowards(agentTransform.rotation, _targetRotation, _agent.angularSpeed * Time.deltaTime);
    }

    private bool IsFacingTarget()
    {
        // Quaternion == uses dot product internally, not exact floating-point comparison
        return _agent.transform.rotation == _targetRotation;
    }
}
