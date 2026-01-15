using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Acquire Autonomy Target", story: "Acquire New Autonomy Target for [AutonomyController]", category: "Action", id: "167d046837f2e744ca67b7f70e288be9")]
public partial class AcquireAutonomyTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<AutonomyController> AutonomyController;

    protected override Status OnStart()
    {
        if (AutonomyController.Value == null)
        {
            return Status.Failure;
        }

        if (!AutonomyController.Value.AcquireNewAutonomyTarget())
        {
            return Status.Failure;
        }

        return Status.Success;
    }
}
