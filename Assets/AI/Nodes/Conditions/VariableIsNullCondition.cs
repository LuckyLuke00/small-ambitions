using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "Variable is Null",
    story: "[Variable] is Null",
    category: "Variable Conditions",
    id: "ee2c5e3dd7cabb097d28d25f34d0a280")]
public partial class VariableIsNullCondition : Condition
{
    /// <summary>
    /// The blackboard variable that is being compared.
    /// </summary>
    [SerializeReference] public BlackboardVariable Variable;

    public override bool IsTrue()
    {
        return Variable == null;
    }
}
