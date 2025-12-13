using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    public static class SmartObjectGizmos
    {
        private const float _sphereGizmoRadius = 0.1f;
        private const float _orientationLineLength = 0.25f;

        private const float _colorSaturation = 0.9f;
        private const float _colorValue = 0.9f;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawSmartObjectGizmos(SmartObject smartObject, GizmoType gizmoType)
        {
            if (smartObject.IKTargets == null)
            {
                return;
            }

            foreach (var ikTarget in smartObject.IKTargets)
            {
                if (!ikTarget.IsValid())
                {
                    continue;
                }

                DrawIKTarget(ikTarget);
            }

            if (smartObject.StandingSpot != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(smartObject.StandingSpot.position, 0.1f);
            }
        }

        private static void DrawIKTarget(IKTarget ikTarget)
        {
            // Anchor
            Gizmos.color = GetColorForType(ikTarget.Type);
            Gizmos.DrawSphere(ikTarget.Position, _sphereGizmoRadius);

            // Orientation
            DrawOrientationAxes(ikTarget.Transform, _orientationLineLength);
        }

        private static void DrawOrientationAxes(Transform transform, float size)
        {
            if (transform == null)
            {
                Debug.LogWarning("Transform is null");
                return;
            }

            Vector3 position = transform.position;

            // X-axis (right) - red
            Gizmos.color = Color.red;
            Gizmos.DrawLine(position, position + transform.right * size);

            // Y-axis (up) - green
            Gizmos.color = Color.green;
            Gizmos.DrawLine(position, position + transform.up * size);

            // Z-axis (forward) - blue
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(position, position + transform.forward * size);
        }

        private static void DrawStandingSpot(Transform transform)
        {
            if (transform == null)
            {
                Debug.LogWarning("Transform is null");
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, _sphereGizmoRadius);
        }

        private static Color GetColorForType(IKTargetType type)
        {
            int index = (int)type;
            int totalTypes = System.Enum.GetValues(typeof(IKTargetType)).Length - 1; // -1 to Ignore IK_None

            float hue = (index % totalTypes) / (float)totalTypes;
            return Color.HSVToRGB(hue, _colorSaturation, _colorValue);
        }
    }
}
