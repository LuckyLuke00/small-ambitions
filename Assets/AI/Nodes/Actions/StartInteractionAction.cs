using SmallAmbitions;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StartInteraction", story: "[Agent] starts interaction with [SmartObject]", category: "Action", id: "3bc74617c614d97383d3dbc66b5eeeed")]
public partial class StartInteractionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<SmartObject> SmartObject;

    private InteractionController _controller;
    private bool _isFinished;

    protected override Status OnStart()
    {
        if (Agent.Value == null || SmartObject.Value == null)
        {
            return Status.Failure;
        }

        if (!Agent.Value.TryGetComponent(out _controller))
        {
            return Status.Failure;
        }

        _isFinished = false;

        _controller.OnInteractionFinished += HandleFinish;

        _controller.StartInteraction(SmartObject.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return _isFinished ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        if (_controller != null)
        {
            _controller.OnInteractionFinished -= HandleFinish;

            if (!_isFinished)
                _controller.StopInteraction(SmartObject.Value);
        }

        _controller = null;
    }

    private void HandleFinish()
    {
        _isFinished = true;
    }
}
