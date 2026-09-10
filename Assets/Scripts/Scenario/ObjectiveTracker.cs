using System;
using System.Collections.Generic;
using System.Linq;

namespace VRSim.Scenario
{
    /// <summary>One step the user has to carry out to finish the drill.</summary>
    public class Objective
    {
        public Objective(string id, string title, string hint)
        {
            Id = id;
            Title = title;
            Hint = hint;
        }

        /// <summary>Stable identifier used by the scenario to mark it done.</summary>
        public string Id { get; }

        /// <summary>Short instruction shown in the objective list.</summary>
        public string Title { get; }

        /// <summary>Where to go or what to look for, shown while this step is current.</summary>
        public string Hint { get; }

        public bool IsComplete { get; internal set; }
    }

    /// <summary>
    /// The checklist a first-time user follows to complete the scenario.
    ///
    /// Section 15 calls for usability testing where first-time users understand the controls,
    /// objectives, signs and feedback without being told what to do. An explicit, visible list of
    /// objectives is what makes that possible.
    /// </summary>
    public class ObjectiveTracker
    {
        readonly List<Objective> m_Objectives;

        public ObjectiveTracker(IEnumerable<Objective> objectives)
        {
            m_Objectives = objectives.ToList();
        }

        /// <summary>Raised whenever an objective actually changes to complete.</summary>
        public event Action Changed;

        public IReadOnlyList<Objective> Objectives => m_Objectives;

        public int CompletedCount => m_Objectives.Count(o => o.IsComplete);

        public bool AllComplete => m_Objectives.All(o => o.IsComplete);

        /// <summary>The first unfinished step, or null once everything is done.</summary>
        public Objective Current => m_Objectives.FirstOrDefault(o => !o.IsComplete);

        /// <summary>Reads as "2/4" for the HUD.</summary>
        public string Progress => $"{CompletedCount}/{m_Objectives.Count}";

        /// <summary>The steps of the fire evacuation drill, in the order a user should do them.</summary>
        public static ObjectiveTracker CreateFireEvacuationObjectives() => new ObjectiveTracker(new[]
        {
            new Objective("read_map", "Read the evacuation map",
                "The white board on the right-hand wall of the hallway."),
            new Objective("raise_alarm", "Raise the fire alarm",
                "The red box on the left-hand wall, opposite the map."),
            new Objective("avoid_hazard", "Keep out of the smoke",
                "The south end of the hallway is filling with smoke. Do not walk into it."),
            new Objective("reach_exit", "Leave by the safe exit",
                "Follow the green exit sign at the north end of the hallway."),
        });

        /// <summary>Marks a step done. Unknown ids and repeats are ignored.</summary>
        public void Complete(string id)
        {
            var objective = m_Objectives.FirstOrDefault(o => o.Id == id);
            if (objective == null || objective.IsComplete)
                return;

            objective.IsComplete = true;
            Changed?.Invoke();
        }

        /// <summary>Clears every step so the drill can be replayed.</summary>
        public void Reset()
        {
            foreach (var objective in m_Objectives)
                objective.IsComplete = false;

            Changed?.Invoke();
        }
    }
}
