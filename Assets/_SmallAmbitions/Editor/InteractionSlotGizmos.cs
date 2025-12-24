using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SmallAmbitions.Editor
{
    [InitializeOnLoad]
    public static class InteractionSlotGizmos
    {
        private const float GizmoSize = 0.15f;
        private const float OrientationLineLength = 0.25f;

        private const float ColorSaturation = 0.9f;
        private const float ColorValue = 0.9f;

        private static readonly Dictionary<Transform, InteractionSlotType> InteractionSlots = new();
        private static readonly InteractionSlotType[] SlotTypes = (InteractionSlotType[])System.Enum.GetValues(typeof(InteractionSlotType));
        private static readonly List<Transform> KeysToRemove = new();

        static InteractionSlotGizmos()
        {
            InteractionSlots.Clear();

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Find all SmartObjects
            InteractionSlots.Clear();

            var smartObjects = UnityEngine.Object.FindObjectsByType<SmartObject>(FindObjectsSortMode.None);
            foreach (var smartObject in smartObjects)
            {
                foreach (var (slotType, slot) in smartObject.InteractionSlots)
                {
                    Register(slot, slotType);
                }
            }
        }

        private static bool ShouldDrawGizmos()
        {
            // Only draw gizmos if a SmartObject or any of its child are selected
            var selected = Selection.activeTransform;
            return selected != null && selected.GetComponentInParent<SmartObject>() != null;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            RemoveDestroyedTransforms();

            if (!ShouldDrawGizmos() || InteractionSlots.Count == 0)
            {
                return;
            }

            // Make button hover highlighting work in editor mode
            if (Event.current.type == EventType.MouseMove)
            {
                sceneView.Repaint();
            }

            var previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;

            foreach (var (slot, slotType) in InteractionSlots)
            {
                DrawSlotHandle(slot, slotType);
            }

            Handles.zTest = previousZTest;
        }

        private static void DrawSlotHandle(Transform slot, InteractionSlotType slotType)
        {
            Handles.color = GetColorForSlotType(slotType);
            if (Handles.Button(slot.position, Quaternion.identity, GizmoSize, GizmoSize, Handles.SphereHandleCap))
            {
                Selection.activeGameObject = slot.gameObject;
            }
            DrawOrientationAxes(slot);
        }

        public static void Register(Transform slot, InteractionSlotType slotType)
        {
            if (slot != null)
            {
                InteractionSlots[slot] = slotType;
            }
        }

        public static void Unregister(Transform slot)
        {
            InteractionSlots.Remove(slot);
        }

        private static void DrawOrientationAxes(Transform transform)
        {
            Vector3 position = transform.position;

            Handles.color = Color.red;
            Handles.DrawLine(position, position + transform.right * OrientationLineLength);

            Handles.color = Color.green;
            Handles.DrawLine(position, position + transform.up * OrientationLineLength);

            Handles.color = Color.blue;
            Handles.DrawLine(position, position + transform.forward * OrientationLineLength);
        }

        private static Color GetColorForSlotType(InteractionSlotType slotType)
        {
            int index = System.Array.IndexOf(SlotTypes, slotType);

            if (index < 0)
            {
                return Color.white;
            }

            float hue = index / (float)SlotTypes.Length;
            return Color.HSVToRGB(hue, ColorSaturation, ColorValue);
        }

        private static void RemoveDestroyedTransforms()
        {
            KeysToRemove.Clear();
            foreach (var key in InteractionSlots.Keys)
            {
                if (key == null)
                {
                    KeysToRemove.Add(key);
                }
            }

            foreach (var key in KeysToRemove)
            {
                InteractionSlots.Remove(key);
            }
        }
    }
}
