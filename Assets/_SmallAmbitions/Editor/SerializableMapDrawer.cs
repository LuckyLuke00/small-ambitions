using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomPropertyDrawer(typeof(SerializableMap<,>))]
    public sealed class SerializableMapDrawer : PropertyDrawer
    {
        private const string EntriesPropertyName = "_entries";
        private const string KeyPropertyName = "Key";

        private static readonly GUIContent DuplicateWarning = EditorGUIUtility.TrTextContentWithIcon(
                    "Duplicate keys detected. Only the first occurrence will be kept at runtime.",
                    "console.warnicon.sml");

        private static readonly GUIContent UnsupportedKeyError = EditorGUIUtility.TrTextContentWithIcon(
                    "Unsupported key type. Use only int, bool, string, enum or char.",
                    "console.erroricon.sml");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float listHeight = EditorGUI.GetPropertyHeight(property, label, true);
            Rect listRect = new(position.x, position.y, position.width, listHeight);

            EditorGUI.PropertyField(listRect, property, label, true);

            SerializedProperty entries = property.FindPropertyRelative(EntriesPropertyName);
            if (HasEntries(entries))
            {
                if (!IsSupportedKeyType(FindFirstKey(entries)))
                {
                    DrawHelpBox(listRect, UnsupportedKeyError);
                }
                else if (HasDuplicateKeys(entries))
                {
                    DrawHelpBox(listRect, DuplicateWarning);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);

            SerializedProperty entries = property.FindPropertyRelative(EntriesPropertyName);
            if (HasEntries(entries))
            {
                if (!IsSupportedKeyType(FindFirstKey(entries)))
                {
                    float helpBoxHeight = EditorStyles.helpBox.CalcHeight(UnsupportedKeyError, EditorGUIUtility.currentViewWidth);
                    return baseHeight + EditorGUIUtility.standardVerticalSpacing + helpBoxHeight;
                }
                else if (HasDuplicateKeys(entries))
                {
                    float helpBoxHeight = EditorStyles.helpBox.CalcHeight(DuplicateWarning, EditorGUIUtility.currentViewWidth);
                    return baseHeight + EditorGUIUtility.standardVerticalSpacing + helpBoxHeight;
                }
            }
            return baseHeight;
        }

        private static bool HasEntries(SerializedProperty entries)
        {
            return entries is { isArray: true, arraySize: > 0 };
        }

        private static bool IsSupportedKeyType(SerializedProperty key)
        {
            return TryGetComparableKey(key, out _);
        }

        private static bool HasDuplicateKeys(SerializedProperty entries)
        {
            var seen = new HashSet<object>();
            for (int i = 0; i < entries.arraySize; ++i)
            {
                var key = entries.GetArrayElementAtIndex(i)?.FindPropertyRelative(KeyPropertyName);
                if (key == null || !TryGetComparableKey(key, out var comparableKey))
                {
                    continue;
                }

                if (!seen.Add(comparableKey))
                {
                    return true;
                }
            }
            return false;
        }

        private static SerializedProperty FindFirstKey(SerializedProperty entries)
        {
            for (int i = 0; i < entries.arraySize; ++i)
            {
                var key = entries.GetArrayElementAtIndex(i)?.FindPropertyRelative(KeyPropertyName);
                if (key != null)
                {
                    return key;
                }
            }
            return null;
        }

        private static bool TryGetComparableKey(SerializedProperty keyProperty, out object comparableKey)
        {
            comparableKey = null;

            if (keyProperty == null)
            {
                return false;
            }

            switch (keyProperty.propertyType)
            {
                case SerializedPropertyType.Integer:
                    comparableKey = keyProperty.longValue;
                    return true;

                case SerializedPropertyType.Boolean:
                    comparableKey = keyProperty.boolValue;
                    return true;

                case SerializedPropertyType.String:
                    comparableKey = keyProperty.stringValue;
                    return true;

                case SerializedPropertyType.Enum:
                    comparableKey = keyProperty.intValue;
                    return true;

                case SerializedPropertyType.Character:
                    comparableKey = (char)keyProperty.intValue;
                    return true;

                default:
                    return false;
            }
        }

        private static void DrawHelpBox(Rect previousRect, GUIContent content)
        {
            float width = previousRect.width;
            float height = EditorStyles.helpBox.CalcHeight(content, width);

            Rect helpRect = new(previousRect.x, previousRect.yMax + EditorGUIUtility.standardVerticalSpacing, width, height);
            EditorGUI.HelpBox(helpRect, content);
        }
    }
}
