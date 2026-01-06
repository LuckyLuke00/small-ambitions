using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Start Interaction", story: "[Self] starts interaction with [AutonomyController]'s Autonomy Target", category: "Action", id: "1b48598ef4597361246d30f7b1974cea")]
public partial class StartInteractionAction : Action
{
    [SerializeReference] public BlackboardVariable<InteractionManager> Self;
    [SerializeReference] public BlackboardVariable<AutonomyController> AutonomyController;

    protected override Status OnStart()
    {
        if (Self.Value == null || AutonomyController == null)
        {
            return Status.Failure;
        }

        if (!Self.Value.TryStartInteraction(AutonomyController.Value.CurrentAutonomyTarget))
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return !Self.Value.IsInteracting ? Status.Success : Status.Running;
    }
}
