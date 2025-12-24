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

        private bool _orphanedEntriesDirty = true;
        private ReorderableList _list;

        private void OnEnable()
        {
            var mapProp = serializedObject.FindProperty(InteractionSlotsProperty);
            var entriesProp = mapProp.FindPropertyRelative(EntriesProperty);

            _list = new ReorderableList(serializedObject, entriesProp)
            {
                drawHeaderCallback = OnDrawHeader,
                drawElementCallback = OnDrawElement,
                onAddDropdownCallback = OnAddDropdown,
                onRemoveCallback = OnRemove
            };

            RegisterAllSlots();

            EditorApplication.hierarchyChanged += MarkOrphanedDirty;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= MarkOrphanedDirty;

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

        private void RegisterAllSlots()
        {
            var entries = _list.serializedProperty;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var element = entries.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative(KeyProperty);
                var value = element.FindPropertyRelative(ValueProperty);

                if (value.objectReferenceValue is Transform t && TryGetSlotType(key, out var slotType))
                {
                    InteractionSlotGizmos.Register(t, slotType);
                }
            }
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

        private static bool TryGetEnumDisplayName(SerializedProperty enumProperty, out string displayName)
        {
            if (TryGetSlotType(enumProperty, out var slotType))
            {
                displayName = slotType.ToString();
                return true;
            }

            displayName = "Invalid (removed enum?)";
            return false;
        }

        private static bool TryGetSlotType(SerializedProperty enumProperty, out InteractionSlotType slotType)
        {
            int index = enumProperty.enumValueIndex;

            if (index >= 0 && index < SlotTypes.Length)
            {
                slotType = SlotTypes[index];
                return true;
            }

            slotType = InteractionSlotType.None;
            return false;
        }

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            var entries = list.serializedProperty;

            var used = new HashSet<int>(entries.arraySize);
            for (int i = 0; i < entries.arraySize; ++i)
            {
                used.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative(KeyProperty).enumValueIndex);
            }

            for (int i = 0; i < SlotTypes.Length; ++i)
            {
                var slotType = SlotTypes[i];
                if (slotType == InteractionSlotType.None) // Skip 'None' type as it's not a valid slot
                {
                    continue;
                }

                var content = new GUIContent(slotType.ToString());
                if (used.Contains(i))
                {
                    menu.AddDisabledItem(content);
                }
                else
                {
                    // Must capture in local variable; loop variable would have wrong value when lambda executes
                    int capturedIndex = i;
                    menu.AddItem(content, false, () => AddEntry(capturedIndex));
                }
            }

            menu.ShowAsContext();
        }

        private void AddEntry(int enumIndex)
        {
            serializedObject.Update();

            var entries = _list.serializedProperty;
            int index = entries.arraySize++;

            var element = entries.GetArrayElementAtIndex(index);
            var key = element.FindPropertyRelative(KeyProperty);
            var value = element.FindPropertyRelative(ValueProperty);

            key.enumValueIndex = enumIndex;

            var root = GetOrCreateRoot(((SmartObject)target).transform);
            var go = new GameObject(key.enumDisplayNames[enumIndex]);

            Undo.RegisterCreatedObjectUndo(go, "Create Interaction Slot");
            go.transform.SetParent(root, false);
            value.objectReferenceValue = go.transform;

            if (TryGetSlotType(key, out var slotType))
            {
                InteractionSlotGizmos.Register(go.transform, slotType);
            }

            serializedObject.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }

        private void OnRemove(ReorderableList list)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(list.index);
            var valueProp = element.FindPropertyRelative(ValueProperty);

            if (valueProp.objectReferenceValue is Transform t && t != null)
            {
                InteractionSlotGizmos.Unregister(t);
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
                var value = element.FindPropertyRelative(ValueProperty);

                if (value.objectReferenceValue != null)
                {
                    continue;
                }

                entries.DeleteArrayElementAtIndex(i);
                modified = true;
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
            serializedObject.Update();

            if (_orphanedEntriesDirty)
            {
                if (RemoveOrphanedEntries())
                {
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                }
                _orphanedEntriesDirty = false;
            }

            DrawPropertiesExcluding(serializedObject, InteractionSlotsProperty, "m_Script");
            EditorGUILayout.Space();
            _list?.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void MarkOrphanedDirty()
        {
            _orphanedEntriesDirty = true;
        }
    }
}
