using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Acquire Autonomy Target", story: "Acquire New Autonomy [Target] for [Self]", category: "Action", id: "167d046837f2e744ca67b7f70e288be9")]
public partial class AcquireAutonomyTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<AutonomyController> Self;
    [SerializeReference] public BlackboardVariable<SmartObject> Target;

    protected override Status OnStart()
    {
        if (Self.Value == null)
        {
            return Status.Failure;
        }

        if (!Self.Value.TryGetAutonomyTarget(out var autonomyTarget))
        {
            return Status.Failure;
        }

        Target.Value = autonomyTarget;
        return Status.Success;
    }
}
