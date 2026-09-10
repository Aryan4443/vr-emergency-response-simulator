using UnityEngine;
using VRSim.Scenario;

namespace VRSim.Core
{
    /// <summary>
    /// Alarm, warning, interaction and success audio, matching AudioManager in section 7.
    ///
    /// Every cue that carries meaning also raises a subtitle through <see cref="SubtitleRequested"/>,
    /// because section 13 requires subtitles for critical audio and forbids relying on one channel
    /// alone to communicate an emergency.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Sim/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("Looping alarm source. Left playing while the emergency is active.")]
        [SerializeField] AudioSource m_AlarmSource;

        [Tooltip("One-shot source for warnings, interactions and the success sting.")]
        [SerializeField] AudioSource m_EffectsSource;

        [Header("Clips")]
        [SerializeField] AudioClip m_AlarmLoop;
        [SerializeField] AudioClip m_WarningClip;
        [SerializeField] AudioClip m_InteractionClip;
        [SerializeField] AudioClip m_SuccessClip;
        [SerializeField] AudioClip m_FailureClip;

        [Header("Scenario")]
        [Tooltip("Leave empty to use the Scenario Manager found in the scene.")]
        [SerializeField] ScenarioManager m_ScenarioManager;

        /// <summary>Raised with the text for a cue that must also be readable.</summary>
        public event System.Action<string> SubtitleRequested;

        void Awake()
        {
            m_ScenarioManager ??= FindAnyObjectByType<ScenarioManager>();
            CreateMissingSources();
            SynthesiseMissingClips();
        }

        /// <summary>Builds the two audio sources when the scene has not supplied them.</summary>
        void CreateMissingSources()
        {
            if (m_AlarmSource == null)
            {
                m_AlarmSource = gameObject.AddComponent<AudioSource>();
                m_AlarmSource.playOnAwake = false;
                m_AlarmSource.loop = true;
            }

            if (m_EffectsSource == null)
            {
                m_EffectsSource = gameObject.AddComponent<AudioSource>();
                m_EffectsSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// Falls back to synthesised cues for any clip the scene does not provide, so the drill is
        /// never silent. Assigning a real recording in the inspector takes precedence.
        /// </summary>
        void SynthesiseMissingClips()
        {
            m_AlarmLoop ??= ProceduralAudio.AlarmLoop();
            m_WarningClip ??= ProceduralAudio.Warning();
            m_InteractionClip ??= ProceduralAudio.Interaction();
            m_SuccessClip ??= ProceduralAudio.Success();
            m_FailureClip ??= ProceduralAudio.Failure();
        }

        void OnEnable()
        {
            if (m_ScenarioManager != null)
                m_ScenarioManager.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (m_ScenarioManager != null)
                m_ScenarioManager.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(ScenarioState previous, ScenarioState next)
        {
            switch (next)
            {
                case ScenarioState.Active when previous == ScenarioState.Briefing:
                    StartAlarm();
                    break;
                case ScenarioState.Warning:
                    PlayWarning();
                    break;
                case ScenarioState.Completed:
                    StopAlarm();
                    PlayOneShot(m_SuccessClip, "You reached a safe exit.");
                    break;
                case ScenarioState.Failed:
                    StopAlarm();
                    PlayOneShot(m_FailureClip, "The evacuation failed.");
                    break;
                case ScenarioState.Ready:
                    StopAlarm();
                    break;
            }
        }

        /// <summary>Starts the looping alarm and announces it in text.</summary>
        public void StartAlarm()
        {
            if (m_AlarmSource != null && m_AlarmLoop != null)
            {
                m_AlarmSource.clip = m_AlarmLoop;
                m_AlarmSource.loop = true;
                m_AlarmSource.Play();
            }

            SubtitleRequested?.Invoke("Fire alarm sounding. Evacuate the building.");
        }

        public void StopAlarm()
        {
            if (m_AlarmSource != null && m_AlarmSource.isPlaying)
                m_AlarmSource.Stop();
        }

        public void PlayWarning() =>
            PlayOneShot(m_WarningClip, "Warning: you are in an unsafe area.");

        /// <summary>Feedback for a successful interaction. Deliberately not subtitled.</summary>
        public void PlayInteraction() => PlayOneShot(m_InteractionClip, null);

        void PlayOneShot(AudioClip clip, string subtitle)
        {
            if (m_EffectsSource != null && clip != null)
                m_EffectsSource.PlayOneShot(clip);

            if (!string.IsNullOrEmpty(subtitle))
                SubtitleRequested?.Invoke(subtitle);
        }
    }
}
