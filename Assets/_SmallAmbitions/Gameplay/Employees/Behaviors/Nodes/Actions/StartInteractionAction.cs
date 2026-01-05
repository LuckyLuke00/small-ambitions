using SmallAmbitions;
using System;
using System.Collections.Generic;
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

        // Only interactions offered by THIS smart object
        var smartObject = SmartObject.Value;
        var interactions = smartObject.Interactions;

        if (interactions.Count == 0)
        {
            return Status.Failure;
        }

        // Build local candidates
        var validCandidates = new List<Interaction>();

        foreach (var interaction in interactions)
        {
            if (!smartObject.HasAvailableSlots(interaction.RequiredPrimarySlots))
            {
                continue;
            }

            validCandidates.Add(interaction);
        }

        if (validCandidates.Count == 0)
        {
            return Status.Failure;
        }

        var chosenInteraction = validCandidates[UnityEngine.Random.Range(0, validCandidates.Count)];
        if (!Self.Value.TryStartInteraction(chosenInteraction, smartObject))
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
