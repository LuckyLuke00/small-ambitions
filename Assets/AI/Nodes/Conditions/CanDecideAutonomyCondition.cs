using SmallAmbitions;
using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can Decide Autonomy", story: "[AutonomyController] Allows Decision", category: "Conditions", id: "9e7133e2c3d7de41718c553517873b12")]
public partial class CanDecideAutonomyCondition : Condition
{
    [SerializeReference] public BlackboardVariable<AutonomyController> AutonomyController;

    public override bool IsTrue()
    {
        return AutonomyController.Value != null && AutonomyController.Value.CanDecide();
    }
}
