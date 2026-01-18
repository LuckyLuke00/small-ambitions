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

    [Tooltip("How often this node is allowed to request a new autonomy target.")]
    [SerializeReference, Min(0.05f)] public BlackboardVariable<float> ThinkIntervalSeconds = new BlackboardVariable<float>(.5f);
    [SerializeReference, Min(1)] public BlackboardVariable<int> MaxAcquiresPerFrame = new BlackboardVariable<int>(6);

    private float _nextThinkTime;
    private bool _initialized;

    private static int s_lastFrame = -1;
    private static int s_acquiresThisFrame = 0;

    protected override Status OnStart()
    {
        if (AutonomyController.Value == null)
        {
            return Status.Failure;
        }

        // Randomize initial think time so NPCs don't all evaluate on the same frame.
        if (!_initialized)
        {
            _initialized = true;
            _nextThinkTime = Time.time + UnityEngine.Random.Range(0f, ThinkIntervalSeconds);
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Time.time < _nextThinkTime)
        {
            return Status.Running;
        }

        // Per-frame throttle (prevents small herds from causing a hitch)
        int frame = Time.frameCount;
        if (frame != s_lastFrame)
        {
            s_lastFrame = frame;
            s_acquiresThisFrame = 0;
        }

        if (s_acquiresThisFrame >= MaxAcquiresPerFrame)
        {
            return Status.Running;
        }

        s_acquiresThisFrame++;

        _nextThinkTime = Time.time + ThinkIntervalSeconds + UnityEngine.Random.Range(0f, 0.03f);

        return AutonomyController.Value.AcquireNewAutonomyTarget()
            ? Status.Success
            : Status.Failure;
    }
}
