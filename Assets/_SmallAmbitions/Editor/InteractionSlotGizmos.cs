using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    public static class InteractionSlotGizmos
    {
        private const float GizmoSize = 0.1f;
        private const float OrientationLineLength = 0.15f;

        private const float ColorSaturation = 0.9f;
        private const float ColorValue = 0.9f;

        private static readonly Color ToleranceRadiusColor = new Color(1f, 0.5f, 0f, 0.5f);

        private static readonly InteractionSlotType[] SlotTypes = (InteractionSlotType[])Enum.GetValues(typeof(InteractionSlotType));

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawSmartObjectGizmos(SmartObject smartObject, GizmoType gizmoType)
        {
            if (!ShouldDrawGizmos())
            {
                return;
            }

            foreach (var interactionSlot in smartObject.InteractionSlots)
            {
                var slotType = interactionSlot.SlotType;
                var slot = interactionSlot.SlotTransform;

                if (slot != null)
                {
                    DrawSlotHandle(slot, slotType);
                }
            }

            DrawPositionToleranceRadius(smartObject);
        }

        private static bool ShouldDrawGizmos()
        {
            // Only draw gizmos if a SmartObject or any of its child are selected
            var selected = Selection.activeTransform;
            return selected != null && selected.GetComponentInParent<SmartObject>() != null;
        }

        private static void DrawSlotHandle(Transform slot, InteractionSlotType slotType)
        {
            var prevGizmoColor = Gizmos.color;
            Gizmos.color = GetColorForSlotType(slotType);
            Gizmos.DrawSphere(slot.position, GizmoSize);
            Gizmos.color = prevGizmoColor;

            DrawOrientationAxes(slot);
        }

        private static void DrawOrientationAxes(Transform transform)
        {
            Vector3 position = transform.position;

            var prevHandleColor = Handles.color;

            Handles.color = Color.red;
            Handles.DrawLine(position, position + transform.right * OrientationLineLength);

            Handles.color = Color.green;
            Handles.DrawLine(position, position + transform.up * OrientationLineLength);

            Handles.color = Color.blue;
            Handles.DrawLine(position, position + transform.forward * OrientationLineLength);

            Handles.color = prevHandleColor;
        }

        private static void DrawPositionToleranceRadius(SmartObject smartObject)
        {
            foreach (var interaction in smartObject.Interactions.Where(
                interaction => interaction != null &&
                !MathUtils.IsNearlyZero(interaction.PositionToleranceRadius)))
            {
                var prevGizmoColor = Gizmos.color;
                Gizmos.color = ToleranceRadiusColor;
                Gizmos.DrawWireSphere(smartObject.transform.position, interaction.PositionToleranceRadius);
                Gizmos.color = prevGizmoColor;
            }
        }

        private static Color GetColorForSlotType(InteractionSlotType slotType)
        {
            int index = Array.IndexOf(SlotTypes, slotType);
            float hue = MathUtils.SafeDivide(index, SlotTypes.Length);
            return Color.HSVToRGB(hue, ColorSaturation, ColorValue);
        }
    }
}
