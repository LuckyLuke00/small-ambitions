using System.Collections.Generic;
using UnityEngine;

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
            _lookup = new Dictionary<IKTargetType, Transform>();

            foreach (var entry in _ikTargets)
            {
                if (!_lookup.ContainsKey(entry.Type))
                    _lookup.Add(entry.Type, entry.Target);
            }
        }

        public Transform GetIKTarget(IKTargetType type)
        {
            return _lookup.GetValueOrDefault(type);
        }
    }
}
