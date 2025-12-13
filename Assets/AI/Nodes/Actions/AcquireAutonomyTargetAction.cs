using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Acquire Autonomy Target", story: "Acquire New Autonomy [Target] From [AutonomyController]", category: "Action", id: "0786501c3aebd9eaf464a43bcacf256a")]
public partial class AcquireAutonomyTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<SmartObject> Target;
    [SerializeReference] public BlackboardVariable<AutonomyController> AutonomyController;

    protected override Status OnStart()
    {
        if (AutonomyController.Value == null)
        {
            return Status.Failure;
        }

        if (!AutonomyController.Value.TryGetAutonomyTarget(out var autonomyTarget))
        {
            return Status.Failure;
        }

        Target.Value = autonomyTarget;
        return Status.Success;
    }
}
