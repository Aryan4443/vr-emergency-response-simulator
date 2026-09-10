using System;
using UnityEngine;
using VRSim.Evaluation;

namespace VRSim.Scenario
{
    /// <summary>
    /// Scene-level owner of the scenario, matching ScenarioManager in section 7. Holds the
    /// <see cref="ScenarioSession"/>, drives its clock from the player loop, and re-publishes
    /// its events so UI, audio and logging components can subscribe from the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Sim/Scenario Manager")]
    public class ScenarioManager : MonoBehaviour
    {
        [Header("Scenario")]
        [Tooltip("Identifier stored with every result, for example fire_evacuation_01.")]
        [SerializeField] string m_ScenarioId = "fire_evacuation_01";

        [Tooltip("Time limit in seconds. Zero runs the scenario untimed.")]
        [SerializeField] float m_TimeLimitSeconds = 180f;

        [Tooltip("Write each finished run to local JSON, as described in section 11.")]
        [SerializeField] bool m_StoreResults = true;

        ScenarioSession m_Session;
        ScenarioResultStore m_Store;

        /// <summary>Raised after every accepted state change, with the previous and new state.</summary>
        public event Action<ScenarioState, ScenarioState> StateChanged;

        /// <summary>Raised with the new total whenever the score changes.</summary>
        public event Action<int> ScoreChanged;

        /// <summary>Raised with the run summary when the results screen opens.</summary>
        public event Action<ScenarioResult> ResultReady;

        /// <summary>The run in progress. Created on first access so tests need no scene setup.</summary>
        public ScenarioSession Session
        {
            get
            {
                if (m_Session == null)
                    CreateSession();

                return m_Session;
            }
        }

        public ScenarioState State => Session.State;

        public int Score => Session.Score;

        public float ElapsedSeconds => Session.ElapsedSeconds;

        public float RemainingSeconds => Session.RemainingSeconds;

        void Awake()
        {
            if (m_Session == null)
                CreateSession();
        }

        void Update()
        {
            Session.Tick(Time.deltaTime);
        }

        void CreateSession()
        {
            m_Session = new ScenarioSession(m_ScenarioId, m_TimeLimitSeconds);
            m_Session.StateChanged += (previous, next) => StateChanged?.Invoke(previous, next);
            m_Session.ScoreChanged += total => ScoreChanged?.Invoke(total);
        }

        // ------------------------------------------------------------------ lifecycle

        public void BeginBriefing() => Session.BeginBriefing();

        public void StartScenario() => Session.StartScenario();

        /// <summary>Opens the results screen, stores the run and publishes the summary.</summary>
        public ScenarioResult EnterReview()
        {
            var result = Session.EnterReview();

            if (m_StoreResults)
            {
                m_Store ??= new ScenarioResultStore();
                try
                {
                    m_Store.Save(result);
                }
                catch (Exception exception)
                {
                    // A failed write must never stop the user seeing their results.
                    Debug.LogWarning($"Could not store the scenario result: {exception.Message}", this);
                }
            }

            ResultReady?.Invoke(result);
            return result;
        }

        /// <summary>Clears the run so the user can replay the scenario.</summary>
        public void ResetScenario() => Session.ResetSession();
    }
}
