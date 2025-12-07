using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorParameter))]
    public class AnimatorParameterDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProp = property.FindPropertyRelativeOrFail("_name");
            SerializedProperty hashProp = property.FindPropertyRelativeOrFail("_hash");

            Animator animator = GetAnimatorFromTarget(property);
            GUIContent[] paramNames = GetAnimatorParameters(animator);

            EditorGUI.BeginProperty(position, label, property);

            if (animator == null)
            {
                DrawDisabledPopup(position, label, "No Animator Component");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                DrawDisabledPopup(position, label, "No Animator Controller");
            }
            else if (paramNames.Length == 0)
            {
                DrawDisabledPopup(position, label, "No Parameters");
            }
            else
            {
                string hashText = $"Hash: {hashProp.intValue}";
                Vector2 hashSize = EditorStyles.miniLabel.CalcSize(new GUIContent(hashText));

                Rect popupRect = new Rect(position.x, position.y, position.width - hashSize.x, position.height);
                Rect hashRect = new Rect(popupRect.xMax, position.y, hashSize.x, position.height);

                int selectedIndex = DrawParameterPopup(popupRect, label, nameProp, paramNames);
                nameProp.stringValue = paramNames[selectedIndex].text;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.LabelField(hashRect, hashText, EditorStyles.miniLabel);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        private static Animator GetAnimatorFromTarget(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is MonoBehaviour mb && mb.TryGetComponent(out Animator animator))
            {
                return animator;
            }

            return null;
        }

        private static GUIContent[] GetAnimatorParameters(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController is not AnimatorController controller)
            {
                return System.Array.Empty<GUIContent>();
            }

            return animator.parameters.Select(p => new GUIContent(p.name)).ToArray();
        }

        private static int DrawParameterPopup(Rect position, GUIContent label, SerializedProperty nameProp, GUIContent[] paramNames)
        {
            Debug.Assert(paramNames.Length > 0, "DrawParameterPopup should not be called with an empty array");

            int selectedIndex = System.Array.FindIndex(paramNames, p => p.text == nameProp.stringValue);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, paramNames.Length - 1);

            return EditorGUI.Popup(position, label, selectedIndex, paramNames);
        }

        private static void DrawDisabledPopup(Rect position, GUIContent label, string message)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                var fieldRect = EditorGUI.PrefixLabel(position, label);

                Color oldColor = GUI.color;
                GUI.color = Color.softYellow;

                GUIContent content = new GUIContent(message, EditorGUIUtility.IconContent("console.warnicon.sml").image);
                EditorGUI.LabelField(fieldRect, content, EditorStyles.popup);

                GUI.color = oldColor;
            }
        }
    }
}
