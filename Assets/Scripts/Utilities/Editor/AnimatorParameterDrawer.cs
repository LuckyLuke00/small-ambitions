using SmallAmbitions;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimatorParameter))]
public class AnimatorParameterDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var nameProp = property.FindPropertyRelative("name");
        if (nameProp == null) return;

        var animator = GetAnimatorFromTarget(property);
        string[] paramNames = GetAnimatorParameters(animator);

        EditorGUI.BeginProperty(position, label, property);

        if (animator != null && animator.runtimeAnimatorController is AnimatorController controller && controller.parameters.Length > 0)
        {
            paramNames = controller.parameters
                .Select(p => $"{p.name} ({p.type})")
                .ToArray();
        }

        EditorGUI.BeginProperty(position, label, property);

        using (new EditorGUI.DisabledScope(animator == null || paramNames.Length == 0))
        {
            if (animator == null)
            {
                DrawLabel(position, label, "No Animator component");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                DrawLabel(position, label, "No Animator Controller");
            }
            else if (paramNames.Length == 0)
            {
                DrawLabel(position, label, "No parameters");
            }
            else
            {
                DrawParameterPopup(position, label, nameProp, paramNames);
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

    private Animator GetAnimatorFromTarget(SerializedProperty property)
    {
        if (property.serializedObject.targetObject is MonoBehaviour mb && mb.TryGetComponent(out Animator animator))
        {
            return animator;
        }

        return null;
    }

    private string[] GetAnimatorParameters(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return System.Array.Empty<string>();
        }

        return animator.parameters.Select(p => $"{p.name} ({p.type})").ToArray();
    }

    private void DrawLabel(Rect position, GUIContent label, string message)
    {
        EditorGUI.LabelField(position, label, new GUIContent(message), EditorStyles.popup);
    }

    private void DrawParameterPopup(Rect position, GUIContent label, SerializedProperty nameProp, string[] paramNames)
    {
        string currentName = nameProp.stringValue;
        string cleanCurrent = string.IsNullOrEmpty(currentName) ? string.Empty : currentName.Split(' ')[0];

        int selectedIndex = System.Array.IndexOf(paramNames, paramNames.FirstOrDefault(p => p.StartsWith(cleanCurrent)));
        if (selectedIndex < 0) selectedIndex = 0;

        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, paramNames);

        string selectedWithType = paramNames[selectedIndex];
        string cleanName = selectedWithType.Split(' ')[0];
        nameProp.stringValue = cleanName;
    }
}
