using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using VRSim.Accessibility;
using VRSim.Core;
using VRSim.Evaluation;
using VRSim.Scenario;

namespace VRSim.UI
{
    /// <summary>
    /// Drives the briefing, prompts, warnings, timer and results panels, matching UIManager in
    /// section 7 and the workflow in section 4.
    ///
    /// Warnings carry an icon glyph and wording as well as colour, because section 13 rules out
    /// signalling anything by colour alone.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Sim/Scenario HUD")]
    public class ScenarioHud : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject m_BriefingPanel;
        [SerializeField] GameObject m_HudPanel;
        [SerializeField] GameObject m_ResultsPanel;

        [Header("Text")]
        [SerializeField] TMP_Text m_BriefingText;
        [SerializeField] TMP_Text m_TimerText;
        [SerializeField] TMP_Text m_ScoreText;
        [SerializeField] TMP_Text m_PromptText;
        [SerializeField] TMP_Text m_SubtitleText;
        [SerializeField] TMP_Text m_ResultsText;
        [SerializeField] TMP_Text m_ObjectivesText;

        [Header("Scenario")]
        [SerializeField] ScenarioManager m_ScenarioManager;
        [SerializeField] AccessibilityManager m_AccessibilityManager;
        [SerializeField] AudioManager m_AudioManager;

        readonly Dictionary<TMP_Text, float> m_BaseFontSizes = new Dictionary<TMP_Text, float>();

        float m_SubtitleClearTime;

        void Awake()
        {
            m_ScenarioManager ??= FindAnyObjectByType<ScenarioManager>();
            m_AccessibilityManager ??= FindAnyObjectByType<AccessibilityManager>();
            m_AudioManager ??= FindAnyObjectByType<AudioManager>();

            foreach (var text in AllTexts().Where(t => t != null))
                m_BaseFontSizes[text] = text.fontSize;
        }

        void OnEnable()
        {
            if (m_ScenarioManager != null)
            {
                m_ScenarioManager.StateChanged += OnStateChanged;
                m_ScenarioManager.ScoreChanged += OnScoreChanged;
                m_ScenarioManager.ResultReady += ShowResults;
            }

            if (m_AccessibilityManager != null)
                m_AccessibilityManager.SettingsApplied += ApplyAccessibility;

            if (m_AudioManager != null)
                m_AudioManager.SubtitleRequested += ShowSubtitle;
        }

        void OnDisable()
        {
            if (m_ScenarioManager != null)
            {
                m_ScenarioManager.StateChanged -= OnStateChanged;
                m_ScenarioManager.ScoreChanged -= OnScoreChanged;
                m_ScenarioManager.ResultReady -= ShowResults;
            }

            if (m_AccessibilityManager != null)
                m_AccessibilityManager.SettingsApplied -= ApplyAccessibility;

            if (m_AudioManager != null)
                m_AudioManager.SubtitleRequested -= ShowSubtitle;
        }

        void Start()
        {
            ShowBriefing();

            if (m_AccessibilityManager != null)
                ApplyAccessibility(m_AccessibilityManager.Settings);

            if (m_ScenarioManager != null)
            {
                Objectives.Changed += RefreshObjectives;
                RefreshObjectives();
            }
        }

        void OnDestroy()
        {
            if (m_ScenarioManager != null)
                Objectives.Changed -= RefreshObjectives;
        }

        /// <summary>
        /// Draws the checklist so the user always knows what is left. A tick and a cross carry the
        /// state as well as the colour, per section 13.
        /// </summary>
        void RefreshObjectives()
        {
            if (m_ObjectivesText == null)
                return;

            var tracker = Objectives;
            var text = $"<b>Objectives {tracker.Progress}</b>\n";

            foreach (var objective in tracker.Objectives)
            {
                var isCurrent = ReferenceEquals(objective, tracker.Current);

                // Plain ASCII on purpose. The bundled LiberationSans has no tick, arrow or warning
                // glyph, and a missing glyph renders as an empty box, which reads as a broken build
                // rather than as a checklist.
                var mark = objective.IsComplete ? "[x]" : isCurrent ? " > " : "[ ]";
                var line = objective.IsComplete
                    ? $"<s>{objective.Title}</s>"
                    : isCurrent ? $"<b>{objective.Title}</b>" : objective.Title;

                text += $"{mark} {line}\n";
            }

            var current = tracker.Current;
            if (current != null)
                text += $"\n<size=85%>{current.Hint}</size>";

            m_ObjectivesText.text = text;
        }

        void Update()
        {
            if (m_ScenarioManager == null)
                return;

            if (m_TimerText != null)
            {
                var remaining = m_ScenarioManager.RemainingSeconds;
                m_TimerText.text = remaining > 0f
                    ? $"Time left  {ScenarioReport.FormatTime(remaining)}"
                    : $"Time  {ScenarioReport.FormatTime(m_ScenarioManager.ElapsedSeconds)}";
            }

            if (m_SubtitleText != null && m_SubtitleClearTime > 0f && Time.time >= m_SubtitleClearTime)
            {
                m_SubtitleText.text = string.Empty;
                m_SubtitleClearTime = 0f;
            }
        }

        IEnumerable<TMP_Text> AllTexts()
        {
            yield return m_BriefingText;
            yield return m_TimerText;
            yield return m_ScoreText;
            yield return m_PromptText;
            yield return m_SubtitleText;
            yield return m_ResultsText;
            yield return m_ObjectivesText;
        }

        // -------------------------------------------------------------------- panels

        /// <summary>Section 4.1: objective, controls, comfort settings and the disclaimer.</summary>
        public void ShowBriefing()
        {
            SetPanels(briefing: true, hud: false, results: false);

            // Showing the panel is not enough: the scenario itself has to enter Briefing, because
            // section 8 only allows Active to be reached from there. Without this the start button
            // swaps the panels while the run stays in Ready, so the clock never moves.
            m_ScenarioManager?.BeginBriefing();

            if (m_BriefingText == null)
                return;

            m_BriefingText.text = BriefingCopy();
        }

        /// <summary>
        /// The instructions a first-time user needs: what to do, in what order, and which control
        /// does it. Section 15 expects someone to complete the drill without being coached.
        /// </summary>
        string BriefingCopy()
        {
            var steps = string.Empty;
            var number = 1;
            foreach (var objective in Objectives.Objectives)
                steps += $"{number++}.  <b>{objective.Title}</b>\n     <size=85%>{objective.Hint}</size>\n";

            return
                "<b>FIRE EVACUATION DRILL</b>\n" +
                "<size=85%>You are in a classroom. A fire has started in the building. " +
                "Get out safely.</size>\n\n" +
                "<b>How to finish this drill</b>\n" +
                steps + "\n" +
                "<b>Controls</b>\n" +
                "<size=85%>W A S D — walk     Mouse — look around\n" +
                "E — use what you are looking at     Q — left hand\n" +
                "In the headset: point a controller and pull the trigger.</size>\n\n" +
                "<size=80%>Scoring rewards raising the alarm, reading the map and taking the safe " +
                $"exit. Entering smoke or the wrong exit loses points.\n\n{ScenarioReport.Disclaimer}</size>";
        }

        ObjectiveTracker Objectives => m_ScenarioManager.Session.Objectives;

        /// <summary>Called by the briefing panel's start button.</summary>
        public void StartScenario()
        {
            if (m_ScenarioManager == null)
                return;

            // Guards against the button being pressed before the briefing was shown.
            if (m_ScenarioManager.State == ScenarioState.Ready)
                m_ScenarioManager.BeginBriefing();

            m_ScenarioManager.StartScenario();

            if (m_ScenarioManager.State != ScenarioState.Active)
            {
                Debug.LogWarning($"Could not start the drill from {m_ScenarioManager.State}.", this);
                return;
            }

            SetPanels(briefing: false, hud: true, results: false);
        }

        void ShowResults(ScenarioResult result)
        {
            SetPanels(briefing: false, hud: false, results: true);

            if (m_ResultsText == null)
                return;

            var summary = string.Join("\n", ScenarioReport.SummaryLines(result));
            var guidance = string.Join("\n", ScenarioReport.Guidance(result).Select(g => "• " + g));

            m_ResultsText.text =
                $"<b>{ScenarioReport.Headline(result)}</b>\n\n" +
                $"{summary}\n\n" +
                $"<b>What to try next time</b>\n{guidance}\n\n" +
                $"<size=80%>{ScenarioReport.Disclaimer}</size>";
        }

        /// <summary>Called by the results panel's replay button.</summary>
        public void Replay()
        {
            m_ScenarioManager.ResetScenario();
            ShowBriefing();
            RefreshObjectives();
        }

        void SetPanels(bool briefing, bool hud, bool results)
        {
            if (m_BriefingPanel != null) m_BriefingPanel.SetActive(briefing);
            if (m_HudPanel != null) m_HudPanel.SetActive(hud);
            if (m_ResultsPanel != null) m_ResultsPanel.SetActive(results);
        }

        // -------------------------------------------------------------------- events

        void OnStateChanged(ScenarioState previous, ScenarioState next)
        {
            if (m_PromptText != null)
            {
                // The glyph and the wording carry the warning, so the message still reads
                // correctly for a user who cannot distinguish the colour.
                // Wording carries the meaning; the prefix is a second, non-colour cue that the
                // bundled font can actually draw.
                m_PromptText.text = next switch
                {
                    ScenarioState.Active => "Find a safe exit.",
                    ScenarioState.Warning => "WARNING - unsafe area. Leave the smoke.",
                    ScenarioState.Completed => "SAFE - you reached a safe exit.",
                    ScenarioState.Failed => "FAILED - the evacuation failed.",
                    _ => string.Empty,
                };
            }

            RefreshObjectives();

            if (next is ScenarioState.Completed or ScenarioState.Failed)
                m_ScenarioManager.EnterReview();
        }

        void OnScoreChanged(int total)
        {
            if (m_ScoreText != null)
                m_ScoreText.text = $"Score  {total}";
        }

        void ShowSubtitle(string line)
        {
            if (m_SubtitleText == null || m_AccessibilityManager == null)
                return;

            if (!m_AccessibilityManager.Settings.SubtitlesEnabled)
                return;

            m_SubtitleText.text = line;
            m_SubtitleClearTime = Time.time + 4f;
        }

        void ApplyAccessibility(AccessibilitySettings settings)
        {
            foreach (var pair in m_BaseFontSizes)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.fontSize = pair.Value * settings.TextScale;
                pair.Key.color = settings.HighContrast ? Color.white : new Color(0.92f, 0.92f, 0.95f);
            }

            if (m_SubtitleText != null && !settings.SubtitlesEnabled)
                m_SubtitleText.text = string.Empty;
        }
    }
}
