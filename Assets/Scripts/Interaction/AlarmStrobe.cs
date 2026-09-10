using UnityEngine;
using VRSim.Scenario;

namespace VRSim.Interaction
{
    /// <summary>
    /// The warning light beside the fire alarm, required by section 9.
    ///
    /// Section 13 rules out sudden flashing effects, so this pulses smoothly at roughly one cycle
    /// a second rather than strobing. The point is to draw the eye toward the emergency, not to
    /// startle or to risk triggering photosensitivity.
    /// </summary>
    [AddComponentMenu("VR Sim/Alarm Strobe")]
    public class AlarmStrobe : MonoBehaviour
    {
        [SerializeField] Light m_Light;
        [SerializeField] Renderer m_Renderer;

        [Tooltip("Pulses per second. Kept low deliberately; this is not a strobe.")]
        [SerializeField] float m_PulsesPerSecond = 1.1f;

        [SerializeField] ScenarioManager m_ScenarioManager;

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        MaterialPropertyBlock m_Block;
        Color m_LitColour = new Color(1f, 0.85f, 0.4f);
        float m_BaseIntensity;
        bool m_Active;

        public Light Light
        {
            get => m_Light;
            set => m_Light = value;
        }

        public Renderer Renderer
        {
            get => m_Renderer;
            set => m_Renderer = value;
        }

        /// <summary>True while the warning light is running.</summary>
        public bool IsActive => m_Active;

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            m_ScenarioManager ??= FindAnyObjectByType<ScenarioManager>();

            if (m_Light != null)
            {
                m_BaseIntensity = m_Light.intensity;
                m_LitColour = m_Light.color;
            }

            SetActive(false);
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
            // The emergency is over once the user is out or the run has ended.
            if (next is ScenarioState.Completed or ScenarioState.Failed or ScenarioState.Ready)
                SetActive(false);
        }

        /// <summary>Switches the warning light on. Called when the alarm is triggered.</summary>
        public void Activate() => SetActive(true);

        public void SetActive(bool active)
        {
            m_Active = active;

            if (m_Light != null)
                m_Light.enabled = active;

            if (!active)
                ApplyEmission(Color.black);
        }

        void Update()
        {
            if (!m_Active)
                return;

            // A smooth sine pulse rather than a hard on/off flash.
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * m_PulsesPerSecond * 2f * Mathf.PI);
            var eased = Mathf.Lerp(0.25f, 1f, pulse);

            if (m_Light != null)
                m_Light.intensity = m_BaseIntensity * eased;

            ApplyEmission(m_LitColour * eased * 2f);
        }

        void ApplyEmission(Color colour)
        {
            if (m_Renderer == null)
                return;

            m_Renderer.GetPropertyBlock(m_Block);
            m_Block.SetColor(EmissionColor, colour);
            m_Renderer.SetPropertyBlock(m_Block);
        }
    }
}
