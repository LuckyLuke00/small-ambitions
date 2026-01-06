using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorParameter))]
    public class AnimatorParameterDrawer : PropertyDrawer
    {
        private const string _nameField = "_name";
        private const string _hashField = "_hash";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty nameProp = property.FindPropertyRelative(_nameField);
            SerializedProperty hashProp = property.FindPropertyRelative(_hashField);

            if (nameProp == null || hashProp == null)
            {
                DrawWarningMessage(position, label, $"Missing \"{_nameField}\" or \"{_hashField}\" field");
                EditorGUI.EndProperty();
                return;
            }

            Animator animator = GetAnimatorFromTarget(property);
            GUIContent[] paramOptions = GetAnimatorParameterOptions(animator);
            string[] paramNames = paramOptions.Select(p => p.text).ToArray();

            if (animator == null)
            {
                DrawWarningMessage(position, label, "No Animator Component");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                DrawWarningMessage(position, label, "No Animator Controller");
            }
            else if (paramOptions.Length == 0)
            {
                DrawWarningMessage(position, label, "No Parameters");
            }
            else
            {
                int selectedIndex = DrawParameterPopupWithHash(position, label, nameProp, hashProp, paramOptions);
                string selectedName = paramNames[selectedIndex];

                nameProp.stringValue = selectedName;
                hashProp.intValue = Animator.StringToHash(selectedName);

                property.serializedObject.ApplyModifiedProperties();
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

        private static GUIContent[] GetAnimatorParameterOptions(Animator animator)
        {
            if (animator != null)
            {
                return animator.parameters.Select(p => new GUIContent(p.name)).ToArray();
            }

            return Array.Empty<GUIContent>();
        }

        private static int GetSelectedParameterIndex(SerializedProperty nameProp, GUIContent[] paramNames)
        {
            return (paramNames != null) ? Array.FindIndex(paramNames, p => p.text == nameProp.stringValue) : -1;
        }

        private static int DrawParameterPopupWithHash(Rect position, GUIContent label, SerializedProperty nameProp, SerializedProperty hashProp, GUIContent[] paramNames)
        {
            string hashText = $"Hash: {hashProp.intValue}";
            Vector2 hashSize = EditorStyles.miniLabel.CalcSize(new GUIContent(hashText));

            Rect popupRect = new Rect(position.x, position.y, position.width - hashSize.x, position.height);
            Rect hashRect = new Rect(popupRect.xMax, position.y, hashSize.x, position.height);

            int selectedIndex = GetSelectedParameterIndex(nameProp, paramNames);
            if (!paramNames.IsValidIndex(selectedIndex))
            {
                return 0;
            }

            int newIndex = EditorGUI.Popup(popupRect, label, selectedIndex, paramNames);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.LabelField(hashRect, hashText, EditorStyles.miniLabel);
            }

            return newIndex;
        }

        private static void DrawWarningMessage(Rect position, GUIContent label, string message)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                var fieldRect = EditorGUI.PrefixLabel(position, label);

                Color oldColor = GUI.color;
                GUI.color = Color.softYellow;

                Texture icon = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                GUIContent content = EditorGUIUtility.TrTextContentWithIcon(message, icon);
                EditorGUI.LabelField(fieldRect, content, EditorStyles.popup);

                GUI.color = oldColor;
            }
        }
    }
}
