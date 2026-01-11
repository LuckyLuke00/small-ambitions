using System.Collections.Generic;
using UnityEngine;

namespace SmallAmbitions
{
    public sealed class MotiveComponent : MonoBehaviour
    {
        [Header("Motives")]
        [SerializeField] private MotiveSettings _motiveSettings;

        private Dictionary<MotiveType, Motive> _motives;

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
            foreach (var motive in _motives.Values)
            {
                motive.Tick(deltaTime);
            }
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

                GUI.Label(new Rect(10f, y, 300f, lineHeight), $"{type}: {motive.CurrentValue:0.0}");

                y += lineHeight;
            }
        }
    }
}
