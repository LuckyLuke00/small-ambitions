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
            if (entries == null || !entries.isArray || entries.arraySize < 2)
            {
                return false;
            }

            for (int i = 0; i < entries.arraySize; ++i)
            {
                var key = entries.GetArrayElementAtIndex(i)?.FindPropertyRelative(KeyFieldName);
                if (key == null)
                {
                    continue;
                }

                for (int j = i + 1; j < entries.arraySize; ++j)
                {
                    var otherKey = entries.GetArrayElementAtIndex(j)?.FindPropertyRelative(KeyFieldName);
                    if (otherKey != null && SerializedProperty.DataEquals(key, otherKey))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
