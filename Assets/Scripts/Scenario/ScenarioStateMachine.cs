using System;
using System.Collections.Generic;

namespace VRSim.Scenario
{
    /// <summary>
    /// Owns the scenario lifecycle described in section 8 of the specification.
    ///
    ///   Ready -> Briefing -> Active
    ///   Active -> Warning -> Active
    ///   Active -> Completed -> Review
    ///   Active -> Failed -> Review
    ///   Review -> Reset -> Ready
    ///
    /// Reset is additionally reachable from every state, because section 13 requires a pause menu
    /// with an immediate exit: abandoning a run has to work wherever the user happens to be.
    /// </summary>
    public class ScenarioStateMachine
    {
        static readonly IReadOnlyDictionary<ScenarioState, ScenarioState[]> AllowedTransitions =
            new Dictionary<ScenarioState, ScenarioState[]>
            {
                { ScenarioState.Ready, new[] { ScenarioState.Briefing } },
                { ScenarioState.Briefing, new[] { ScenarioState.Active } },
                { ScenarioState.Active, new[] { ScenarioState.Warning, ScenarioState.Completed, ScenarioState.Failed } },
                { ScenarioState.Warning, new[] { ScenarioState.Active } },
                { ScenarioState.Completed, new[] { ScenarioState.Review } },
                { ScenarioState.Failed, new[] { ScenarioState.Review } },
                { ScenarioState.Review, new[] { ScenarioState.Reset } },
                { ScenarioState.Reset, new[] { ScenarioState.Ready } },
            };

        /// <summary>Raised after an accepted transition, with the previous and the new state.</summary>
        public event Action<ScenarioState, ScenarioState> StateChanged;

        public ScenarioState CurrentState { get; private set; } = ScenarioState.Ready;

        /// <summary>
        /// Moves to <paramref name="next"/> when the transition is legal.
        /// Returns false and leaves the current state untouched otherwise.
        /// </summary>
        public bool TryTransitionTo(ScenarioState next)
        {
            if (!AllowedTransitions.TryGetValue(CurrentState, out var allowed))
                return false;

            // Abandoning a run is always permitted, from any state.
            if (next != ScenarioState.Reset && Array.IndexOf(allowed, next) < 0)
                return false;

            var previous = CurrentState;
            CurrentState = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }
    }
}
