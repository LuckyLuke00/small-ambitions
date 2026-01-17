using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    /// <summary>
    /// Visual debugger overlay for the Interaction System. Shows debug info in the top-left corner
    /// of the Scene View when NPCs are selected. Multiple selected NPCs stack horizontally.
    /// </summary>
    public static class InteractionSystemDebugOverlay
    {
        private const float PanelWidth = 280f;
        private const float PanelPadding = 10f;
        private const float PanelSpacing = 10f;
        private const float RefreshInterval = 0.1f;
        private const int MaxWeightedInteractionsToShow = 5;

        private static readonly MotiveType[] _motiveTypes = (MotiveType[])System.Enum.GetValues(typeof(MotiveType));

        private static GUIStyle _labelStyle;
        private static GUIStyle _backgroundStyle;
        private static GUIStyle _criticalBackgroundStyle;
        private static Texture2D _backgroundTexture;
        private static Texture2D _criticalBackgroundTexture;
        private static readonly StringBuilder _stringBuilder = new();
        private static readonly Dictionary<int, CachedDebugInfo> _cache = new();
        private static readonly HashSet<int> _currentFrameIds = new();
        private static double _lastRefreshTime;
        private static bool _stylesInitialized;
        private static bool _styleInitAttempted;

        private struct CachedDebugInfo
        {
            public string Content;
            public bool HasCritical;
            public GUIContent GUIContent;
            public Vector2 ContentSize;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Cleanup when exiting or entering edit mode to prevent resource leaks during domain reloads
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                _cache.Clear();
                CleanupStyles();
                _stylesInitialized = false;
                _styleInitAttempted = false;
            }
        }

        private static void CleanupStyles()
        {
            // Null out style references before destroying textures they reference
            _backgroundStyle = null;
            _criticalBackgroundStyle = null;
            _labelStyle = null;

            if (_backgroundTexture != null)
            {
                Object.DestroyImmediate(_backgroundTexture);
                _backgroundTexture = null;
            }

            if (_criticalBackgroundTexture != null)
            {
                Object.DestroyImmediate(_criticalBackgroundTexture);
                _criticalBackgroundTexture = null;
            }
        }

        private static void OnSceneGUI(SceneView _)
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return;
            }

            bool shouldRefresh = EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval;
            if (shouldRefresh)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }

            EnsureStylesInitialized();
            if (!_stylesInitialized)
            {
                return;
            }

            Handles.BeginGUI();

            _currentFrameIds.Clear();
            int panelIndex = 0;
            foreach (var go in selectedObjects)
            {
                if (!TryGetDebugComponents(go, out var motive, out var interaction, out var autonomy))
                {
                    continue;
                }

                int id = go.GetInstanceID();
                _currentFrameIds.Add(id);

                if (shouldRefresh || !_cache.TryGetValue(id, out var cached))
                {
                    string content = BuildDebugContent(go.name, motive, interaction, autonomy);
                    var guiContent = new GUIContent(content);
                    float availableWidth = PanelWidth - _backgroundStyle.padding.left - _backgroundStyle.padding.right;
                    float contentHeight = _labelStyle.CalcHeight(guiContent, availableWidth);
                    cached = new CachedDebugInfo
                    {
                        Content = content,
                        HasCritical = motive != null && motive.HasCriticalMotive(),
                        GUIContent = guiContent,
                        ContentSize = new Vector2(availableWidth, contentHeight)
                    };
                    _cache[id] = cached;
                }

                DrawPanel(panelIndex++, cached);
            }

            // Remove cache entries for objects no longer selected
            if (shouldRefresh)
            {
                var keysToRemove = _cache.Keys.Where(k => !_currentFrameIds.Contains(k)).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
            }

            Handles.EndGUI();
        }

        private static bool TryGetDebugComponents(GameObject go, out MotiveComponent motive, out InteractionManager interaction, out AutonomyController autonomy)
        {
            motive = go.GetComponent<MotiveComponent>();
            interaction = go.GetComponent<InteractionManager>();
            autonomy = go.GetComponent<AutonomyController>();
            return motive != null || interaction != null || autonomy != null;
        }

        private static void DrawPanel(int panelIndex, CachedDebugInfo cached)
        {
            if (string.IsNullOrEmpty(cached.Content))
            {
                return;
            }

            GUIStyle bgStyle = cached.HasCritical ? _criticalBackgroundStyle : _backgroundStyle;
            float xPos = PanelPadding + panelIndex * (PanelWidth + PanelSpacing);

            float panelHeight = cached.ContentSize.y + bgStyle.padding.top + bgStyle.padding.bottom;

            Rect backgroundRect = new(xPos, PanelPadding, PanelWidth, panelHeight);
            Rect labelRect = new(
                xPos + bgStyle.padding.left,
                PanelPadding + bgStyle.padding.top,
                PanelWidth - bgStyle.padding.left - bgStyle.padding.right,
                cached.ContentSize.y
            );

            GUI.Box(backgroundRect, GUIContent.none, bgStyle);
            GUI.Label(labelRect, cached.GUIContent, _labelStyle);
        }

        private static string BuildDebugContent(string name, MotiveComponent motive, InteractionManager interaction, AutonomyController autonomy)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("<b><size=14>").Append(name).AppendLine("</size></b>");

            AppendCriticalWarning(motive);
            AppendMotives(motive);
            AppendWeightedInteractions(motive, interaction);
            AppendCurrentInteraction(interaction, autonomy);
            AppendActiveObjects(autonomy);

            return _stringBuilder.ToString().TrimEnd();
        }

        private static void AppendCriticalWarning(MotiveComponent motive)
        {
            if (motive == null || !motive.HasCriticalMotive())
            {
                return;
            }

            if (motive.TryGetCriticalMotive(out MotiveType criticalType))
            {
                _stringBuilder.Append("<color=#FF4444><b>⚠ CRITICAL: ").Append(criticalType).AppendLine("</b></color>");
            }
        }

        private static void AppendMotives(MotiveComponent motive)
        {
            _stringBuilder.AppendLine("\n<b>Motives</b>");

            if (motive == null)
            {
                _stringBuilder.AppendLine("  (No MotiveComponent)");
                return;
            }

            motive.TryGetCriticalMotive(out MotiveType criticalType);
            bool hasCritical = motive.HasCriticalMotive();

            foreach (MotiveType motiveType in _motiveTypes)
            {
                if (!motive.TryGetMotive(motiveType, out Motive m))
                {
                    continue;
                }

                float normalized = 1f - motive.GetNormalizedMotiveValue(motiveType);
                bool isCritical = motiveType == criticalType && hasCritical;

                _stringBuilder.Append("  ").Append(motiveType).Append(": <color=")
                    .Append(GetMotiveColor(normalized, isCritical)).Append(">")
                    .Append(BuildProgressBar(normalized, 10)).Append("</color> ")
                    .Append(m.CurrentValue.ToString("F0")).Append("/").Append(m.MaxValue.ToString("F0"))
                    .AppendLine(isCritical ? " ⚠" : "");
            }
        }

        private static string GetMotiveColor(float normalized, bool isCritical)
        {
            return isCritical ? "#FF4444" : normalized < 0.3f ? "#FF6666" : normalized < 0.6f ? "#FFCC66" : "#66FF66";
        }

        private static void AppendWeightedInteractions(MotiveComponent motive, InteractionManager interaction)
        {
            _stringBuilder.AppendLine("\n<b>Available Interactions</b>");

            if (interaction == null || motive == null)
            {
                _stringBuilder.AppendLine("  (Missing references)");
                return;
            }

            if (!interaction.TryGetAvailableInteractions(out List<InteractionCandidate> candidates))
            {
                _stringBuilder.AppendLine("  (None)");
                return;
            }

            var scored = candidates
                .Select(c => (c.Interaction?.name ?? "null", c.SmartObject?.name ?? "null", ScoreInteraction(c.Interaction, motive)))
                .OrderByDescending(x => x.Item3)
                .Take(MaxWeightedInteractionsToShow);

            bool first = true;
            foreach (var (interactionName, obj, score) in scored)
            {
                _stringBuilder.Append(first ? "  ★ " : "    ").Append(interactionName)
                    .Append(" @ ").Append(obj).Append(" (").Append(score.ToString("+0.00;-0.00;0.00")).AppendLine(")");
                first = false;
            }
        }

        private static void AppendCurrentInteraction(InteractionManager interaction, AutonomyController autonomy)
        {
            _stringBuilder.AppendLine("\n<b>Current Interaction</b>");

            if (autonomy == null)
            {
                _stringBuilder.AppendLine("  (No AutonomyController)");
                return;
            }

            var target = autonomy.CurrentAutonomyTarget;
            if (target.Interaction == null)
            {
                _stringBuilder.AppendLine("  (Idle)");
                return;
            }

            _stringBuilder.Append("  ").AppendLine(target.Interaction.name);
            _stringBuilder.Append("  Phase: ");
            if (interaction != null && interaction.IsInteracting)
            {
                _stringBuilder.Append(interaction.DebugPrimaryPhase).Append(" / ").AppendLine(interaction.DebugAmbientPhase);
            }
            else
            {
                _stringBuilder.AppendLine("Idle");
            }
            _stringBuilder.Append("  Status: ").AppendLine(autonomy.HasReservedTarget ? "Reserved" : "Active");
        }

        private static void AppendActiveObjects(AutonomyController autonomy)
        {
            _stringBuilder.AppendLine("\n<b>Active Objects</b>");

            if (autonomy == null)
            {
                _stringBuilder.AppendLine("  (No AutonomyController)");
                return;
            }

            var target = autonomy.CurrentAutonomyTarget;
            _stringBuilder.Append("  Primary: ").AppendLine(target.PrimarySmartObject?.name ?? "(None)");
            _stringBuilder.Append("  Ambient: ").AppendLine(target.AmbientSmartObject?.name ?? "(None)");
        }

        private static float ScoreInteraction(Interaction interaction, MotiveComponent motive)
        {
            if (motive == null || interaction == null)
            {
                return 0f;
            }

            float score = 0f;
            foreach (var mod in interaction.MotiveDecayRates)
            {
                score += mod.Value * motive.GetNormalizedMotiveValue(mod.Key);
            }
            return score;
        }

        private static string BuildProgressBar(float normalized, int length)
        {
            int filled = Mathf.RoundToInt(normalized * length);
            return new string('█', filled) + new string('░', length - filled);
        }

        private static void EnsureStylesInitialized()
        {
            if (_stylesInitialized)
            {
                return;
            }

            if (GUI.skin == null)
            {
                if (!_styleInitAttempted)
                {
                    _styleInitAttempted = true;
                    Debug.LogWarning("[InteractionSystemDebugOverlay] GUI.skin is null, cannot initialize styles.");
                }
                return;
            }

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
            _backgroundTexture.Apply();

            _backgroundStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
                normal = { background = _backgroundTexture }
            };

            _criticalBackgroundTexture = new Texture2D(1, 1);
            _criticalBackgroundTexture.SetPixel(0, 0, new Color(0.4f, 0f, 0f, 0.9f));
            _criticalBackgroundTexture.Apply();

            _criticalBackgroundStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
                normal = { background = _criticalBackgroundTexture }
            };

            _stylesInitialized = true;
        }
    }
}
