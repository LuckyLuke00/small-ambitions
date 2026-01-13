using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class MotiveComponent : MonoBehaviour
    {
        [Header("Motives")]
        [SerializeField] private MotiveSettings _motiveSettings;

        [Header("Critical Threshold")]
        [Tooltip("Normalized threshold (0-1) below which a motive is considered critical.")]
        [SerializeField, Range(0f, 1f)] private float _criticalThreshold = 0.1f;

        private Dictionary<MotiveType, Motive> _motives;

        public event Action<MotiveType> OnMotiveCritical;

        private void Awake()
        {
            _motives = new Dictionary<MotiveType, Motive>(_motiveSettings.Motives.Count);
            foreach (var pair in _motiveSettings.Motives)
            {
                // Deep copy so each agent owns its own state
                _motives[pair.Key] = new Motive(pair.Value);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var pair in _motives)
            {
                var motive = pair.Value;
                bool wasCritical = motive.IsCritical(_criticalThreshold);

                motive.Tick(deltaTime);

                // Check if motive just became critical
                if (!wasCritical && motive.IsCritical(_criticalThreshold))
                {
                    OnMotiveCritical?.Invoke(pair.Key);
                }
            }
        }

        public bool TryGetMotive(MotiveType type, out Motive motive)
        {
            return _motives.TryGetValue(type, out motive);
        }

        /// <summary>
        /// Returns true if any motive is at or below the critical threshold.
        /// </summary>
        public bool HasCriticalMotive()
        {
            foreach (var motive in _motives.Values)
            {
                if (motive.IsCritical(_criticalThreshold))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetCriticalMotive(out MotiveType criticalType)
        {
            foreach (var pair in _motives)
            {
                if (pair.Value.IsCritical(_criticalThreshold))
                {
                    criticalType = pair.Key;
                    return true;
                }
            }
            criticalType = default;
            return false;
        }

        public float GetNormalizedMotiveValue(MotiveType type)
        {
            if (!_motives.TryGetValue(type, out var motive))
            {
                return 0f; // Unknown motive = no urgency
            }

            float range = motive.MaxValue - motive.MinValue;
            if (range <= 0f)
            {
                return 0f; // Invalid range = no urgency
            }

            // Invert so empty motives (low CurrentValue) return high urgency (close to 1)
            return 1f - (motive.CurrentValue - motive.MinValue) / range;
        }

        public void ApplyMotiveModifiers(IReadOnlyDictionary<MotiveType, float> modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                if (_motives.TryGetValue(modifier.Key, out var motive))
                {
                    motive.AddRateModifier(modifier.Value);
                }
            }
        }

        public void RemoveMotiveModifiers(IReadOnlyDictionary<MotiveType, float> modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                if (_motives.TryGetValue(modifier.Key, out var motive))
                {
                    motive.RemoveRateModifier(modifier.Value);
                }
            }
        }

        // TODO: Move this to a proper UI system
        private void OnGUI()
        {
            const float lineHeight = 20f;
            float y = 10f;

            GUI.Label(new Rect(10f, y, 300f, lineHeight), "Motives");
            y += lineHeight;

            foreach (var pair in _motives)
            {
                var type = pair.Key;
                var motive = pair.Value;

                string criticalLabel = motive.IsCritical(_criticalThreshold) ? " [CRITICAL]" : "";
                GUI.Label(new Rect(10f, y, 300f, lineHeight), $"{type}: {motive.CurrentValue:0.0}{criticalLabel}");

                y += lineHeight;
            }
        }
    }
}
