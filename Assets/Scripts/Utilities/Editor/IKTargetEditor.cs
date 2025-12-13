using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    [CustomEditor(typeof(SmartObject))]
    public sealed class IKTargetEditor : UnityEditor.Editor
    {
        private const string _ikTargetsPropName = "_ikTargets";
        private const string _ikTargetRootName = "IK Targets";

        private ReorderableList _list;
        private SerializedProperty _ikTargetsProp;

        private void OnEnable()
        {
            _ikTargetsProp = serializedObject.FindProperty(_ikTargetsPropName);

            _list = new ReorderableList(serializedObject, _ikTargetsProp, true, true, true, true);

            _list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "IK Targets");

            _list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _ikTargetsProp.GetArrayElementAtIndex(index);
                // Handle [field: SerializeField] backing fields
                var typeProp = element.FindPropertyRelative("<Type>k__BackingField") ??
                               element.FindPropertyRelative("Type");
                var targetProp = element.FindPropertyRelative("<Target>k__BackingField") ??
                                 element.FindPropertyRelative("Target");

                string label = typeProp.enumDisplayNames[typeProp.enumValueIndex];

                Rect labelRect = new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

                Rect fieldRect = new Rect(rect.x + 100, rect.y, rect.width - 100, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(fieldRect, targetProp, GUIContent.none);
            };

            _list.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                var menu = new GenericMenu();
                var script = (SmartObject)target;
                var allTypes = System.Enum.GetValues(typeof(IKTargetType)).Cast<IKTargetType>();
                var usedTypes = script.IKTargets.Select(x => x.Type).ToList();
                var available = allTypes.Except(usedTypes).Where(t => t != IKTargetType.IK_None);

                if (!available.Any()) menu.AddDisabledItem(new GUIContent("All Types Assigned"));
                else
                    foreach (var type in available)
                        menu.AddItem(new GUIContent(type.ToString()), false, OnAddHandler, type);

                menu.ShowAsContext();
            };

            // --- THE REMOVE LOGIC (List -> Scene) ---
            _list.onRemoveCallback = (ReorderableList l) =>
            {
                var element = _ikTargetsProp.GetArrayElementAtIndex(l.index);
                var targetProp = element.FindPropertyRelative("<Target>k__BackingField") ??
                                 element.FindPropertyRelative("Target");

                // If the GameObject exists, destroy it
                if (targetProp.objectReferenceValue != null)
                {
                    Transform t = (Transform)targetProp.objectReferenceValue;
                    Undo.DestroyObjectImmediate(t.gameObject);
                }

                // Remove from list
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
            };
        }

        private void OnAddHandler(object targetEnum)
        {
            IKTargetType type = (IKTargetType)targetEnum;
            SmartObject script = (SmartObject)target;

            GameObject newObj = new GameObject($"{type}");
            newObj.transform.SetParent(GetOrCreateIKRoot(script), false);

            Undo.RegisterCreatedObjectUndo(newObj, "Create IK Target");
            Undo.RecordObject(script, "Add IK Target");

            script.IKTargets.Add(new IKTarget(type, newObj.transform));
            Selection.activeGameObject = newObj;
        }

        private static Transform GetOrCreateIKRoot(SmartObject script)
        {
            Transform ikTargetRoot = script.transform.Find(_ikTargetRootName);
            if (ikTargetRoot != null)
            {
                return ikTargetRoot;
            }

            GameObject newRootObj = new GameObject(_ikTargetRootName);
            Undo.RegisterCreatedObjectUndo(newRootObj, "Create IK Targets Root");

            newRootObj.transform.SetParent(script.transform, false);
            newRootObj.transform.localPosition = Vector3.zero;
            newRootObj.transform.localRotation = Quaternion.identity;

            return newRootObj.transform;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- THE CLEANUP LOGIC (Scene -> List) ---
            // Iterate backwards to safely remove nulls
            SmartObject script = (SmartObject)target;
            bool listDirty = false;

            for (int i = script.IKTargets.Count - 1; i >= 0; --i)
            {
                // If the Target Transform is missing (null), it means the user deleted the GameObject in the scene
                if (script.IKTargets[i].Target == null)
                {
                    script.IKTargets.RemoveAt(i);
                    listDirty = true;
                }
            }

            if (listDirty)
            {
                // Force the SerializedObject to sync with the underlying script change
                serializedObject.Update();
                EditorUtility.SetDirty(script);
            }

            DrawPropertiesExcluding(serializedObject, _ikTargetsPropName, "m_Script");
            GUILayout.Space(10);
            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
