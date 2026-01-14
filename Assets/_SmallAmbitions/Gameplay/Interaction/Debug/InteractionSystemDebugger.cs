#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SmallAmbitions
{
    /// <summary>
    /// Visual debugger for the Interaction System. Shows debug info in the top-left corner
    /// when the NPC is selected. Multiple selected NPCs stack horizontally.
    /// Editor-only component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionSystemDebugger : MonoBehaviour
    {
        private const float PanelWidth = 280f;
        private const float PanelPadding = 10f;
        private const float PanelSpacing = 10f;

        [Header("References")]
        [SerializeField] private MotiveComponent _motiveComponent;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private AutonomyController _autonomyController;

        [Header("Content Options")]
        [SerializeField] private bool _showMotives = true;
        [SerializeField] private bool _showWeightedInteractions = true;
        [SerializeField] private bool _showCurrentInteraction = true;
        [SerializeField] private bool _showActiveObjects = true;
        [SerializeField] private int _maxWeightedInteractionsToShow = 5;

        private static readonly List<InteractionSystemDebugger> _activeDebuggers = new();
        private static GUIStyle _labelStyle;
        private static GUIStyle _backgroundStyle;
        private static GUIStyle _criticalBackgroundStyle;
        private readonly StringBuilder _stringBuilder = new();

        private void OnEnable() => _activeDebuggers.Add(this);
        private void OnDisable() => _activeDebuggers.Remove(this);

        private void OnGUI()
        {
            if (!IsSelected())
            {
                return;
            }

            int panelIndex = GetPanelIndex();
            if (panelIndex < 0)
            {
                return;
            }

            EnsureStylesInitialized();

            string content = BuildDebugContent();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            bool hasCritical = _motiveComponent != null && _motiveComponent.HasCriticalMotive();
            GUIStyle bgStyle = hasCritical ? _criticalBackgroundStyle : _backgroundStyle;

            float xPos = PanelPadding + panelIndex * (PanelWidth + PanelSpacing);
            Rect panelRect = new(xPos, PanelPadding, PanelWidth, Screen.height - PanelPadding * 2);

            GUILayout.BeginArea(panelRect);
            GUILayout.BeginVertical(bgStyle);
            GUILayout.Label(content, _labelStyle);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private bool IsSelected()
        {
            return Selection.Contains(gameObject);
        }

        private int GetPanelIndex()
        {
            int index = 0;
            foreach (var debugger in _activeDebuggers)
            {
                if (debugger == this)
                {
                    return index;
                }

                if (debugger.IsSelected())
                {
                    index++;
                }
            }
            return -1;
        }

        private static void EnsureStylesInitialized()
        {
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            if (_backgroundStyle == null)
            {
                _backgroundStyle = new GUIStyle(GUI.skin.box);
                Texture2D bgTex = new(1, 1);
                bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
                bgTex.Apply();
                _backgroundStyle.normal.background = bgTex;
                _backgroundStyle.padding = new RectOffset(8, 8, 8, 8);
            }

            if (_criticalBackgroundStyle == null)
            {
                _criticalBackgroundStyle = new GUIStyle(GUI.skin.box);
                Texture2D criticalTex = new(1, 1);
                criticalTex.SetPixel(0, 0, new Color(0.4f, 0f, 0f, 0.9f));
                criticalTex.Apply();
                _criticalBackgroundStyle.normal.background = criticalTex;
                _criticalBackgroundStyle.padding = new RectOffset(8, 8, 8, 8);
            }
        }

        private string BuildDebugContent()
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine($"<b><size=14>{gameObject.name}</size></b>");

            AppendCriticalWarning();

            if (_showMotives) AppendMotives();
            if (_showWeightedInteractions) AppendWeightedInteractions();
            if (_showCurrentInteraction) AppendCurrentInteraction();
            if (_showActiveObjects) AppendActiveObjects();

            return _stringBuilder.ToString().TrimEnd();
        }

        private void AppendCriticalWarning()
        {
            if (_motiveComponent == null || !_motiveComponent.HasCriticalMotive())
            {
                return;
            }

            if (_motiveComponent.TryGetCriticalMotive(out MotiveType criticalType))
            {
                _stringBuilder.AppendLine($"<color=#FF4444><b>⚠ CRITICAL: {criticalType}</b></color>");
            }
        }

        private void AppendMotives()
        {
            _stringBuilder.AppendLine("\n<b>Motives</b>");

            if (_motiveComponent == null)
            {
                _stringBuilder.AppendLine("  (No MotiveComponent)");
                return;
            }

            _motiveComponent.TryGetCriticalMotive(out MotiveType criticalType);

            foreach (MotiveType motiveType in System.Enum.GetValues(typeof(MotiveType)))
            {
                if (_motiveComponent.TryGetMotive(motiveType, out Motive motive))
                {
                    float normalized = 1f - _motiveComponent.GetNormalizedMotiveValue(motiveType);
                    bool isCritical = motiveType == criticalType && _motiveComponent.HasCriticalMotive();

                    string bar = BuildProgressBar(normalized, 10);
                    string color = GetMotiveColor(normalized, isCritical);
                    string criticalMarker = isCritical ? " ⚠" : "";

                    _stringBuilder.AppendLine($"  {motiveType}: <color={color}>{bar}</color> {motive.CurrentValue:F0}/{motive.MaxValue:F0}{criticalMarker}");
                }
            }
        }

        private static string GetMotiveColor(float normalized, bool isCritical)
        {
            if (isCritical) return "#FF4444";
            if (normalized < 0.3f) return "#FF6666";
            if (normalized < 0.6f) return "#FFCC66";
            return "#66FF66";
        }

        private void AppendWeightedInteractions()
        {
            _stringBuilder.AppendLine("\n<b>Available Interactions</b>");

            if (_interactionManager == null || _motiveComponent == null)
            {
                _stringBuilder.AppendLine("  (Missing references)");
                return;
            }

            if (!_interactionManager.TryGetAvailableInteractions(out List<InteractionCandidate> candidates))
            {
                _stringBuilder.AppendLine("  (None)");
                return;
            }

            var scored = candidates
                .Select(c => (c.Interaction?.name ?? "null", c.SmartObject?.name ?? "null", ScoreInteraction(c.Interaction)))
                .OrderByDescending(x => x.Item3)
                .Take(_maxWeightedInteractionsToShow);

            bool first = true;
            foreach (var (interaction, obj, score) in scored)
            {
                string marker = first ? "★" : "  ";
                _stringBuilder.AppendLine($"  {marker} {interaction} @ {obj} ({score:+0.00;-0.00;0.00})");
                first = false;
            }
        }

        private void AppendCurrentInteraction()
        {
            _stringBuilder.AppendLine("\n<b>Current Interaction</b>");

            if (_autonomyController == null)
            {
                _stringBuilder.AppendLine("  (No AutonomyController)");
                return;
            }

            var target = _autonomyController.CurrentAutonomyTarget;

            if (target.Interaction == null)
            {
                _stringBuilder.AppendLine("  (Idle)");
                return;
            }

            _stringBuilder.AppendLine($"  {target.Interaction.name}");
            _stringBuilder.AppendLine($"  Phase: {GetCurrentPhase()}");
            _stringBuilder.AppendLine($"  Status: {(_autonomyController.HasReservedTarget ? "Reserved" : "Active")}");
        }

        private void AppendActiveObjects()
        {
            _stringBuilder.AppendLine("\n<b>Active Objects</b>");

            if (_autonomyController == null)
            {
                _stringBuilder.AppendLine("  (No AutonomyController)");
                return;
            }

            var target = _autonomyController.CurrentAutonomyTarget;
            _stringBuilder.AppendLine($"  Primary: {target.PrimarySmartObject?.name ?? "(None)"}");
            _stringBuilder.AppendLine($"  Ambient: {target.AmbientSmartObject?.name ?? "(None)"}");
        }

        private string GetCurrentPhase()
        {
            if (_interactionManager == null || !_interactionManager.IsInteracting)
            {
                return "Idle";
            }

            return $"{_interactionManager.DebugPrimaryPhase} / {_interactionManager.DebugAmbientPhase}";
        }

        private float ScoreInteraction(Interaction interaction)
        {
            if (_motiveComponent == null || interaction == null) return 0f;

            float score = 0f;
            foreach (var mod in interaction.MotiveDecayRates)
            {
                score += mod.Value * _motiveComponent.GetNormalizedMotiveValue(mod.Key);
            }
            return score;
        }

        private static string BuildProgressBar(float normalized, int length)
        {
            int filled = Mathf.RoundToInt(normalized * length);
            return new string('█', filled) + new string('░', length - filled);
        }

        private void Reset()
        {
            _motiveComponent = GetComponent<MotiveComponent>();
            _interactionManager = GetComponent<InteractionManager>();
            _autonomyController = GetComponent<AutonomyController>();
        }
    }
}

#endif