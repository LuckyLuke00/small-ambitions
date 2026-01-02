using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Start Interaction", story: "[Self] starts interaction with [SmartObject]", category: "Action", id: "1b48598ef4597361246d30f7b1974cea")]
public partial class StartInteractionAction : Action
{
    [SerializeReference] public BlackboardVariable<InteractionManager> Self;
    [SerializeReference] public BlackboardVariable<SmartObject> SmartObject;

    protected override Status OnStart()
    {
        if (Self.Value == null || SmartObject.Value == null)
        {
            return Status.Failure;
        }

        if (!Self.Value.TryGetAvailableInteractions(out var availableInteractions) || availableInteractions.Count == 0)
        {
            return Status.Failure;
        }

        var interactionCandidate = availableInteractions[UnityEngine.Random.Range(0, availableInteractions.Count)];
        if (!Self.Value.TryStartInteraction(interactionCandidate.Interaction, interactionCandidate.SmartObject))
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
