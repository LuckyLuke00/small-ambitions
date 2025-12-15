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

        [Tooltip("How long the interaction lasts (in seconds) before animations are ready.")]
        [SerializeField] private float _interactionTime = 3.0f;

        [Header("IK Setup")]
        [SerializeField] private List<IKTarget> _ikTargets = new List<IKTarget>();

        public float InteractionTime => _interactionTime;
        public Transform StandingSpot => _standingSpot;
        public List<IKTarget> IKTargets => _ikTargets;

        private Dictionary<IKTargetType, Transform> _lookup;

        private void Awake()
        {
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

        public Transform GetIKTarget(IKTargetType type)
        {
            return _lookup.GetValueOrDefault(type);
        }
    }
}
