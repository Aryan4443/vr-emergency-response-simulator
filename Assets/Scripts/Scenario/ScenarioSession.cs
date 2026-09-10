using System;
using System.Collections.Generic;
using VRSim.Evaluation;
using VRSim.Scenario.Definitions;

namespace VRSim.Scenario
{
    /// <summary>
    /// Drives one run of a scenario: the state machine from section 8, the clock and score from
    /// sections 7 and 10, and the anonymous action log from section 11.
    ///
    /// Deliberately free of Unity types so the whole scenario flow can be unit tested. The
    /// MonoBehaviour layer forwards headset interactions to the methods below and listens to the
    /// events for feedback.
    /// </summary>
    public class ScenarioSession
    {
        readonly string m_ScenarioId;
        readonly ScenarioStateMachine m_StateMachine = new ScenarioStateMachine();
        readonly ScenarioTimer m_Timer;
        readonly ScoreBoard m_ScoreBoard = new ScoreBoard();
        readonly PerformanceLogger m_Logger;
        readonly ObjectiveTracker m_Objectives;

        string m_SelectedExit;

        /// <param name="scenarioId">Identifier stored with the result, e.g. fire_evacuation_01.</param>
        /// <param name="timeLimitSeconds">Time limit. Zero or less runs the scenario untimed.</param>
        /// <param name="objectives">
        /// The checklist for this scenario. Defaults to the fire drill, which keeps the original
        /// two-argument form working for callers that predate multiple scenarios.
        /// </param>
        public ScenarioSession(string scenarioId, float timeLimitSeconds = 0f,
            ObjectiveTracker objectives = null)
        {
            m_ScenarioId = scenarioId;
            m_Timer = new ScenarioTimer(timeLimitSeconds);
            m_Logger = new PerformanceLogger(m_Timer);
            m_Objectives = objectives ?? ObjectiveTracker.CreateFireEvacuationObjectives();

            m_StateMachine.StateChanged += (previous, next) => StateChanged?.Invoke(previous, next);
            m_ScoreBoard.ScoreChanged += total => ScoreChanged?.Invoke(total);
            m_Timer.Expired += OnTimeLimitReached;
        }

        /// <summary>Raised after every accepted state change, with the previous and new state.</summary>
        public event Action<ScenarioState, ScenarioState> StateChanged;

        /// <summary>Raised with the new total whenever the score changes.</summary>
        public event Action<int> ScoreChanged;

        /// <summary>Raised when the run is suspended or resumed.</summary>
        public event Action<bool> PausedChanged;

        /// <summary>
        /// Builds a run from a scenario definition, which is how every scenario beyond the
        /// original fire drill is created: the identity, clock and checklist all come from data.
        /// </summary>
        public static ScenarioSession FromDefinition(ScenarioDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return new ScenarioSession(definition.ScenarioId, definition.TimeLimitSeconds,
                definition.CreateObjectiveTracker());
        }

        public ScenarioState State => m_StateMachine.CurrentState;

        public int Score => m_ScoreBoard.Total;

        public float ElapsedSeconds => m_Timer.ElapsedSeconds;

        public float RemainingSeconds => m_Timer.RemainingSeconds;

        public IReadOnlyList<ActionEvent> Events => m_Logger.Events;

        /// <summary>
        /// True while the run is suspended. Pausing is not a scenario state: the run stays where
        /// it is and simply stops advancing, so resuming cannot lose the user's progress.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>The checklist the user works through to finish the drill.</summary>
        public ObjectiveTracker Objectives => m_Objectives;

        /// <summary>True while the user is expected to be acting inside the scenario.</summary>
        bool IsInteractive =>
            !IsPaused && (State == ScenarioState.Active || State == ScenarioState.Warning);

        // ------------------------------------------------------------------ lifecycle

        /// <summary>Shows the briefing from section 4.1. The clock stays stopped.</summary>
        public void BeginBriefing() => m_StateMachine.TryTransitionTo(ScenarioState.Briefing);

        /// <summary>Starts the scenario and the clock, per section 4.3.</summary>
        public void StartScenario()
        {
            if (!m_StateMachine.TryTransitionTo(ScenarioState.Active))
                return;

            m_Timer.Start();
        }

        /// <summary>Advances the scenario clock. Call once per frame with the frame delta.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsPaused)
                return;

            m_Timer.Tick(deltaSeconds);
        }

        /// <summary>
        /// Suspends the run for the pause menu required by section 13. Only meaningful while the
        /// user is actually in the scenario.
        /// </summary>
        public void Pause()
        {
            if (IsPaused || State is not (ScenarioState.Active or ScenarioState.Warning))
                return;

            IsPaused = true;
            m_Timer.Pause();
            PausedChanged?.Invoke(true);
        }

        /// <summary>Continues a suspended run from exactly where it stopped.</summary>
        public void Resume()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            m_Timer.Resume();
            PausedChanged?.Invoke(false);
        }

        /// <summary>
        /// Leaves the drill immediately and discards the run, for the immediate exit option in
        /// section 13. The partial run is deliberately not scored or stored.
        /// </summary>
        public void Abandon()
        {
            Resume();
            ResetSession();
        }

        /// <summary>Moves to the results screen and returns the run summary from section 4.6.</summary>
        public ScenarioResult EnterReview()
        {
            m_Timer.Pause();
            m_StateMachine.TryTransitionTo(ScenarioState.Review);

            var completed = m_ScoreBoard.CountOf(ScoreEvent.SuccessfulCompletion) > 0;
            return m_Logger.BuildResult(m_ScenarioId, m_ScoreBoard, completed, m_SelectedExit);
        }

        /// <summary>Clears the run so the user can replay the scenario, per section 4.6.</summary>
        public void ResetSession()
        {
            m_StateMachine.TryTransitionTo(ScenarioState.Reset);
            m_StateMachine.TryTransitionTo(ScenarioState.Ready);

            m_Timer.Reset();
            m_ScoreBoard.Reset();
            m_Logger.Reset();
            m_Objectives.Reset();
            m_SelectedExit = null;
        }

        // ---------------------------------------------------------------- interactions

        /// <summary>Fire alarm activated, per section 9.</summary>
        public void ActivateAlarm(string objectId)
        {
            if (RecordInteraction("alarm_activated", objectId, ScoreEvent.AlarmActivated, successful: true))
                m_Objectives.Complete("raise_alarm");
        }

        /// <summary>Evacuation map inspected, per section 9.</summary>
        public void ViewMap(string objectId)
        {
            if (RecordInteraction("map_viewed", objectId, ScoreEvent.EvacuationMapViewed, successful: true))
                m_Objectives.Complete("read_map");
        }

        /// <summary>Hazard correctly recognised without entering it.</summary>
        public void IdentifyHazard(string objectId) =>
            RecordInteraction("hazard_identified", objectId, ScoreEvent.HazardIdentified, successful: true);

        /// <summary>Door opened. Logged for the review but carries no score of its own.</summary>
        public void OpenDoor(string objectId) =>
            RecordInteraction("door_opened", objectId, scoreEvent: null, successful: true);

        /// <summary>User entered an unsafe area, per section 9.</summary>
        public void EnterHazard(string objectId)
        {
            if (!RecordInteraction("hazard_entered", objectId, ScoreEvent.UnsafeAreaEntered, successful: false))
                return;

            m_StateMachine.TryTransitionTo(ScenarioState.Warning);
        }

        /// <summary>User left the unsafe area and the warning clears.</summary>
        public void LeaveHazard(string objectId)
        {
            if (State != ScenarioState.Warning)
                return;

            m_Logger.Record("hazard_exited", objectId, successful: true);
            m_StateMachine.TryTransitionTo(ScenarioState.Active);
        }

        /// <summary>
        /// User reached an exit. The correct one completes the scenario; a wrong one is penalised
        /// and the run continues so the user can recover.
        /// </summary>
        public void ReachExit(string exitId, bool isCorrect)
        {
            if (!IsInteractive)
                return;

            if (!isCorrect)
            {
                RecordInteraction("wrong_exit_selected", exitId, ScoreEvent.WrongExitSelected, successful: false);
                return;
            }

            m_SelectedExit = exitId;
            RecordInteraction("safe_exit_reached", exitId, ScoreEvent.CorrectSafeExit, successful: true);
            m_ScoreBoard.Record(ScoreEvent.SuccessfulCompletion);
            m_Objectives.Complete("reach_exit");

            // Staying out of the smoke is only proven once the user is out of the building.
            if (m_ScoreBoard.CountOf(ScoreEvent.UnsafeAreaEntered) == 0)
                m_Objectives.Complete("avoid_hazard");

            LeaveWarningIfNeeded();
            m_StateMachine.TryTransitionTo(ScenarioState.Completed);
            m_Timer.Pause();
        }

        /// <summary>
        /// Marks a checklist step done for scenarios whose objectives are not tied to one of the
        /// fire drill's interactions, such as taking cover during an earthquake. Ignored unless
        /// the run is accepting input, so a step cannot be completed from the pause menu.
        /// </summary>
        public bool CompleteObjective(string objectiveId)
        {
            if (!IsInteractive)
                return false;

            m_Objectives.Complete(objectiveId);
            return true;
        }

        /// <summary>
        /// Records an action and its score for a scenario-specific interaction, using the same
        /// logging and scoring path as the built-in fire drill interactions.
        /// </summary>
        public bool RecordScenarioAction(string actionType, string objectId, ScoreEvent? scoreEvent,
            bool successful) =>
            RecordInteraction(actionType, objectId, scoreEvent, successful);

        /// <summary>A critical unsafe action, which ends the run in failure per section 8.</summary>
        public void TriggerCriticalUnsafeAction(string objectId)
        {
            if (!RecordInteraction("critical_unsafe_action", objectId, ScoreEvent.CriticalUnsafeAction, successful: false))
                return;

            LeaveWarningIfNeeded();
            m_StateMachine.TryTransitionTo(ScenarioState.Failed);
            m_Timer.Pause();
        }

        // --------------------------------------------------------------------- helpers

        /// <summary>
        /// Logs an interaction and applies its score when the scenario is accepting input.
        /// Returns whether the interaction counted.
        /// </summary>
        bool RecordInteraction(string actionType, string objectId, ScoreEvent? scoreEvent, bool successful)
        {
            if (!IsInteractive)
                return false;

            m_Logger.Record(actionType, objectId, successful);

            if (scoreEvent.HasValue)
                m_ScoreBoard.Record(scoreEvent.Value);

            return true;
        }

        /// <summary>
        /// Completion and failure are only reachable from Active, so a run that ends while the
        /// user is still standing in a hazard has to clear the warning first.
        /// </summary>
        void LeaveWarningIfNeeded()
        {
            if (State == ScenarioState.Warning)
                m_StateMachine.TryTransitionTo(ScenarioState.Active);
        }

        void OnTimeLimitReached()
        {
            LeaveWarningIfNeeded();
            m_StateMachine.TryTransitionTo(ScenarioState.Failed);
        }
    }
}
