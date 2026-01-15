using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomPropertyDrawer(typeof(SerializableSet<>))]
    public sealed class SerializableSetDrawer : PropertyDrawer
    {
        private const string EntriesPropertyName = "_entries";

        private static readonly GUIContent DuplicateWarning = EditorGUIUtility.TrTextContentWithIcon(
                    "Duplicate values detected. Only the first occurrence will be kept at runtime.",
                    "console.warnicon.sml");

        private static readonly GUIContent UnsupportedValueError = EditorGUIUtility.TrTextContentWithIcon(
                    "Unsupported value type. Use only int, bool, string, enum or char.",
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
                if (!IsSupportedValueType(entries.GetArrayElementAtIndex(0)))
                {
                    DrawHelpBox(listRect, UnsupportedValueError);
                }
                else if (HasDuplicateValues(entries))
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
                if (!IsSupportedValueType(entries.GetArrayElementAtIndex(0)))
                {
                    float helpBoxHeight = EditorStyles.helpBox.CalcHeight(UnsupportedValueError, EditorGUIUtility.currentViewWidth);
                    return baseHeight + EditorGUIUtility.standardVerticalSpacing + helpBoxHeight;
                }
                else if (HasDuplicateValues(entries))
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

        private static bool IsSupportedValueType(SerializedProperty value)
        {
            return TryGetComparableValue(value, out _, out _);
        }

        private static bool HasDuplicateValues(SerializedProperty entries)
        {
            var seenNumerics = new HashSet<long>();
            var seenStrings = new HashSet<string>();

            for (int i = 0; i < entries.arraySize; ++i)
            {
                var element = entries.GetArrayElementAtIndex(i);
                if (element == null)
                {
                    return false;
                }

                if (!TryGetComparableValue(element, out var numeric, out var str))
                {
                    continue;
                }

                if (str != null)
                {
                    if (!seenStrings.Add(str))
                        return true;
                }
                else
                {
                    if (!seenNumerics.Add(numeric))
                        return true;
                }
            }
            return false;
        }

        private static bool TryGetComparableValue(SerializedProperty valueProperty, out long numericValue, out string stringValue)
        {
            numericValue = default;
            stringValue = null;

            if (valueProperty == null)
            {
                return false;
            }

            switch (valueProperty.propertyType)
            {
                case SerializedPropertyType.Integer:
                    numericValue = valueProperty.longValue;
                    return true;

                case SerializedPropertyType.Boolean:
                    numericValue = valueProperty.boolValue ? 1L : 0L;
                    return true;

                case SerializedPropertyType.String:
                    stringValue = valueProperty.stringValue;
                    return true;

                case SerializedPropertyType.Enum:
                    numericValue = valueProperty.intValue;
                    return true;

                case SerializedPropertyType.Character:
                    numericValue = valueProperty.intValue;
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
