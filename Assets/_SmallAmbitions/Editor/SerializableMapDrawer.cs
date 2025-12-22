using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomPropertyDrawer(typeof(SerializableMap<,>), useForChildren: true)]
    public sealed class SerializableMapDrawer : PropertyDrawer
    {
        private const string EntriesFieldName = "_entries";
        private const string KeyFieldName = "Key";

        private const string WarningIconName = "console.warnicon.sml";
        private const string WarningMessage = "Duplicate keys detected. Only the first occurrence is used.";

        private static readonly GUIStyle WarningStyle = EditorStyles.helpBox;
        private static readonly GUIContent WarningContent = new(WarningMessage, EditorGUIUtility.IconContent(WarningIconName).image);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            var propertyRect = new Rect(position.x, position.y, position.width, propertyHeight);

            EditorGUI.PropertyField(propertyRect, property, label, true);

            if (!HasDuplicateKeys(property))
            {
                return;
            }

            propertyRect.y = propertyRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            propertyRect.height = GetWarningHeight(position.width);

            EditorGUI.LabelField(propertyRect, WarningContent, WarningStyle);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUI.GetPropertyHeight(property, label, includeChildren: true);

            if (HasDuplicateKeys(property))
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += GetWarningHeight(EditorGUIUtility.currentViewWidth);
            }

            return height;
        }

        private static float GetWarningHeight(float width)
        {
            return WarningStyle.CalcHeight(WarningContent, width);
        }

        private static bool HasDuplicateKeys(SerializedProperty mapProperty)
        {
            var entries = mapProperty.FindPropertyRelative(EntriesFieldName);
            if (entries == null || !entries.isArray || entries.arraySize == 0)
            {
                return false;
            }

            // For primitive types, we can extract values and use a HashSet for O(n) performance
            // For complex types, we fall back to storing SerializedProperty copies
            var firstKey = entries.GetArrayElementAtIndex(0)?.FindPropertyRelative(KeyFieldName);
            if (firstKey == null)
            {
                return false;
            }

            return firstKey.propertyType switch
            {
                SerializedPropertyType.Integer => HasDuplicates(entries, p => p.intValue),
                SerializedPropertyType.Boolean => HasDuplicates(entries, p => p.boolValue),
                SerializedPropertyType.Float => HasDuplicates(entries, p => p.floatValue),
                SerializedPropertyType.String => HasDuplicates(entries, p => p.stringValue),
                SerializedPropertyType.Enum => HasDuplicates(entries, p => p.enumValueIndex),
                SerializedPropertyType.Vector2 => HasDuplicates(entries, p => p.vector2Value),
                SerializedPropertyType.Vector3 => HasDuplicates(entries, p => p.vector3Value),
                SerializedPropertyType.Vector4 => HasDuplicates(entries, p => p.vector4Value),
                SerializedPropertyType.Vector2Int => HasDuplicates(entries, p => p.vector2IntValue),
                SerializedPropertyType.Vector3Int => HasDuplicates(entries, p => p.vector3IntValue),
                SerializedPropertyType.Rect => HasDuplicates(entries, p => p.rectValue),
                SerializedPropertyType.RectInt => HasDuplicates(entries, p => p.rectIntValue),
                SerializedPropertyType.Bounds => HasDuplicates(entries, p => p.boundsValue),
                SerializedPropertyType.BoundsInt => HasDuplicates(entries, p => p.boundsIntValue),
                SerializedPropertyType.Quaternion => HasDuplicates(entries, p => p.quaternionValue),
                SerializedPropertyType.Color => HasDuplicates(entries, p => p.colorValue),
                SerializedPropertyType.Hash128 => HasDuplicates(entries, p => p.hash128Value),
                _ => HasDuplicatesGeneric(entries)
            };
        }

        private static bool HasDuplicates<T>(SerializedProperty entries, System.Func<SerializedProperty, T> getValue)
        {
            var seenKeys = new System.Collections.Generic.HashSet<T>();

            for (int i = 0; i < entries.arraySize; ++i)
            {
                var key = entries.GetArrayElementAtIndex(i)?.FindPropertyRelative(KeyFieldName);
                if (key == null)
                {
                    continue;
                }

                if (!seenKeys.Add(getValue(key)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDuplicatesGeneric(SerializedProperty entries)
        {
            // Fallback for unsupported types: use DataEquals with copied properties
            var seenKeys = new System.Collections.Generic.List<SerializedProperty>();

            for (int i = 0; i < entries.arraySize; ++i)
            {
                var key = entries.GetArrayElementAtIndex(i)?.FindPropertyRelative(KeyFieldName);
                if (key == null)
                {
                    continue;
                }

                foreach (var seenKey in seenKeys)
                {
                    if (SerializedProperty.DataEquals(key, seenKey))
                    {
                        return true;
                    }
                }

                seenKeys.Add(key.Copy());
            }

            return false;
        }
    }
}
