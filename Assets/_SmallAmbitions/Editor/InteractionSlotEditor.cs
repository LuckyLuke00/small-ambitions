using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomEditor(typeof(SmartObject))]
    public sealed class InteractionSlotEditor : UnityEditor.Editor
    {
        private const string InteractionSlotsProperty = "_interactionSlots";
        private const string EntriesProperty = "_entries";
        private const string KeyProperty = "Key";
        private const string ValueProperty = "Value";

        private const string InteractionSlotsLabel = "Interaction Slots";

        private static readonly InteractionSlotType[] SlotTypes = (InteractionSlotType[])Enum.GetValues(typeof(InteractionSlotType));

        private ReorderableList _list;

        private void OnEnable()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            var mapProp = serializedObject.FindProperty(InteractionSlotsProperty);
            if (mapProp == null)
            {
                return;
            }

            var entriesProp = mapProp.FindPropertyRelative(EntriesProperty);
            if (entriesProp == null)
            {
                return;
            }

            _list = new ReorderableList(serializedObject, entriesProp)
            {
                drawHeaderCallback = OnDrawHeader,
                drawElementCallback = OnDrawElement,
                onAddDropdownCallback = OnAddDropdown,
                onRemoveCallback = OnRemove
            };
        }

        private void OnDisable()
        {
            if (_list == null)
            {
                return;
            }

            _list.drawHeaderCallback = null;
            _list.drawElementCallback = null;
            _list.onAddDropdownCallback = null;
            _list.onRemoveCallback = null;
            _list = null;
        }

        private void OnDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, InteractionSlotsLabel);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _list.serializedProperty.GetArrayElementAtIndex(index);
            var key = element.FindPropertyRelative(KeyProperty);
            var value = element.FindPropertyRelative(ValueProperty);

            rect.height = EditorGUIUtility.singleLineHeight;

            Color previousColor = GUI.color;
            if (!TryGetEnumDisplayName(key, out string keyDisplayName))
            {
                GUI.color = Color.softRed;
            }

            Rect fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent(keyDisplayName));
            EditorGUI.PropertyField(fieldRect, value, GUIContent.none);

            GUI.color = previousColor;
        }

        private static bool IsValidInteractionSlotType(int rawValue)
        {
            return Enum.IsDefined(typeof(InteractionSlotType), rawValue);
        }

        private static bool TryGetEnumDisplayName(SerializedProperty enumProperty, out string displayName)
        {
            int rawValue = enumProperty.intValue;

            if (IsValidInteractionSlotType(rawValue))
            {
                displayName = ((InteractionSlotType)rawValue).ToString();
                return true;
            }

            displayName = $"<Invalid ({rawValue})>";
            return false;
        }

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            var entries = list.serializedProperty;

            var used = new HashSet<int>(entries.arraySize);
            for (int i = 0; i < entries.arraySize; ++i)
            {
                var keyProp = entries.GetArrayElementAtIndex(i).FindPropertyRelative(KeyProperty);
                used.Add(keyProp.intValue);
            }

            foreach (var slotType in SlotTypes)
            {
                if (slotType == InteractionSlotType.None) // Skip 'None' type as it's not a valid slot
                {
                    continue;
                }

                int rawValue = (int)slotType;
                var content = new GUIContent(slotType.ToString());

                if (used.Contains(rawValue))
                {
                    menu.AddDisabledItem(content);
                }
                else
                {
                    // Must capture in local variable; loop variable would have wrong value when lambda executes
                    int capturedValue = rawValue;
                    menu.AddItem(content, false, () => AddEntry(capturedValue));
                }
            }

            menu.ShowAsContext();
        }

        private void AddEntry(int rawEnumValue)
        {
            serializedObject.Update();

            var entries = _list.serializedProperty;
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);

            var element = entries.GetArrayElementAtIndex(index);
            var key = element.FindPropertyRelative(KeyProperty);
            var value = element.FindPropertyRelative(ValueProperty);

            value.objectReferenceValue = null;
            key.intValue = rawEnumValue;

            var root = GetOrCreateRoot(((SmartObject)target).transform);

            TryGetEnumDisplayName(key, out string slotName);
            var go = new GameObject(slotName);

            Undo.RegisterCreatedObjectUndo(go, "Create Interaction Slot");
            go.transform.SetParent(root, false);

            value.objectReferenceValue = go.transform;

            serializedObject.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }

        private void OnRemove(ReorderableList list)
        {
            if (list.index < 0 || list.index >= list.serializedProperty.arraySize)
            {
                return;
            }

            var element = list.serializedProperty.GetArrayElementAtIndex(list.index);
            var valueProp = element.FindPropertyRelative(ValueProperty);

            if (valueProp.objectReferenceValue is Transform t && t != null)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private bool RemoveOrphanedEntries()
        {
            var entries = _list.serializedProperty;
            bool modified = false;

            for (int i = entries.arraySize - 1; i >= 0; --i)
            {
                var element = entries.GetArrayElementAtIndex(i);
                if (element == null)
                {
                    continue;
                }

                var valueProp = element.FindPropertyRelative(ValueProperty);
                if (valueProp == null)
                {
                    continue;
                }

                if (valueProp.objectReferenceValue == null)
                {
                    entries.DeleteArrayElementAtIndex(i);
                    modified = true;
                }
            }

            return modified;
        }

        private static Transform GetOrCreateRoot(Transform parent)
        {
            var root = parent.Find(InteractionSlotsLabel);
            if (root != null)
            {
                return root;
            }

            var go = new GameObject(InteractionSlotsLabel);
            Undo.RegisterCreatedObjectUndo(go, "Create Interaction Slots Root");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            serializedObject.Update();

            if (RemoveOrphanedEntries())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            DrawPropertiesExcluding(serializedObject, InteractionSlotsProperty, "m_Script");
            EditorGUILayout.Space();
            _list?.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
