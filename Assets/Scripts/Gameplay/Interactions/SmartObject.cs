using UnityEngine;
using System.Collections.Generic;

namespace SmallAmbitions
{
    public sealed class SmartObject : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private SmartObjectSet _smartObjectSet;
        
        [Header("Settings (Optional)")]
        [Tooltip("The actual object to pick up")]
        [SerializeField] private GameObject _propToGrab;

        [Tooltip("Where the NPC stands. If null, uses this object's transform.")]
        [SerializeField] private Transform _standingSpot;
        
        [Header("IK Setup")]
        [SerializeField] private List<IKTarget> _ikTargets = new List<IKTarget>();

        public List<IKTarget> IKTargets => _ikTargets;
        public Transform StandingSpot => _standingSpot;

        private Dictionary<IKTargetType, Transform> _lookup;

        private void Awake()
        {
            CreateLookup();
            _standingSpot ??= transform;
        }

        private void OnEnable()
        {
            _smartObjectSet.Add(this);
        }

        private void OnDisable()
        {
            _smartObjectSet.Remove(this);
        }

        private void CreateLookup()
        {
            foreach (var entry in _ikTargets)
            {
                _lookup = new Dictionary<IKTargetType, Transform>();
                if (!_lookup.ContainsKey(entry.Type))
                    _lookup.Add(entry.Type, entry.Target);
            }
        }

        public Transform GetIKTarget(IKTargetType type)
        {
            return _lookup.GetValueOrDefault(type);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (StandingSpot != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(StandingSpot.position, 0.1f);
            }
            
            if (_ikTargets == null || _ikTargets.Count == 0)
            {
                return;
            }

            foreach (var target in _ikTargets)
            {
                if (target.Target == null || target.Type == IKTargetType.IK_None)
                {
                    continue;
                }
                
                Gizmos.color = GetColorForType(target.Type);
                Gizmos.DrawSphere(target.Target.position, 0.1f);
            }
        }
        
        private static Color GetColorForType(IKTargetType type)
        {
            int index = (int)type;
            int totalTypes = System.Enum.GetValues(typeof(IKTargetType)).Length - 1; // Minus one to exclude IK_None

            // Evenly distribute hues around the color wheel
            float hue = (index % totalTypes) / (float)totalTypes;

            const float saturation = 0.95f;
            const float value = 0.95f;

            return Color.HSVToRGB(hue, saturation, value);
        }
#endif
    }
}