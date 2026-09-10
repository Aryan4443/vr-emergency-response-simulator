using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRSim.Scenario.Definitions
{
    /// <summary>
    /// Everything that distinguishes one training scenario from another: its identity, the copy
    /// shown before it starts, its clock and its checklist.
    ///
    /// Section 20 asks for earthquake, chemical-spill and medical-emergency drills alongside the
    /// fire evacuation, and for an instructor mode that configures scenarios. Both need the
    /// scenario to be data rather than code, which is what this type is for: no Unity dependency
    /// beyond JSON, so the rules below are unit tested directly.
    ///
    /// Values are held in serialised backing fields because JsonUtility ignores properties.
    /// </summary>
    [Serializable]
    public class ScenarioDefinition
    {
        [SerializeField] string m_ScenarioId;
        [SerializeField] string m_DisplayName;
        [SerializeField] string m_Summary;
        [SerializeField] string m_BriefingIntro;
        [SerializeField] float m_TimeLimitSeconds;
        [SerializeField] ScenarioDifficulty m_Difficulty;
        [SerializeField] List<ObjectiveDefinition> m_Objectives = new List<ObjectiveDefinition>();

        /// <summary>
        /// Identifier stored with every result, e.g. fire_evacuation_01. Results are grouped by it
        /// in the review screen, so it must not change once runs have been recorded against it.
        /// </summary>
        public string ScenarioId
        {
            get => m_ScenarioId;
            set => m_ScenarioId = value;
        }

        /// <summary>Name shown on the level-select screen.</summary>
        public string DisplayName
        {
            get => m_DisplayName;
            set => m_DisplayName = value;
        }

        /// <summary>A sentence or two describing the drill, for the level-select screen.</summary>
        public string Summary
        {
            get => m_Summary;
            set => m_Summary = value;
        }

        /// <summary>
        /// The opening line of the briefing, which tells the user where they are and what has just
        /// happened. The rest of the briefing is generated from the objectives and controls.
        /// </summary>
        public string BriefingIntro
        {
            get => m_BriefingIntro;
            set => m_BriefingIntro = value;
        }

        /// <summary>Time limit in seconds. Zero runs the scenario untimed.</summary>
        public float TimeLimitSeconds
        {
            get => m_TimeLimitSeconds;
            set => m_TimeLimitSeconds = value;
        }

        /// <summary>How demanding the drill is meant to be.</summary>
        public ScenarioDifficulty Difficulty
        {
            get => m_Difficulty;
            set => m_Difficulty = value;
        }

        /// <summary>The checklist, in the order a user should work through it.</summary>
        public IReadOnlyList<ObjectiveDefinition> Objectives => ObjectiveList;

        /// <summary>True when <see cref="Validate"/> finds nothing wrong.</summary>
        public bool IsValid => Validate().Count == 0;

        /// <summary>
        /// JsonUtility rebuilds the instance without running field initialisers, so a stored
        /// definition that omits the array would otherwise leave a null list behind.
        /// </summary>
        List<ObjectiveDefinition> ObjectiveList => m_Objectives ??= new List<ObjectiveDefinition>();

        /// <summary>Appends one step to the end of the checklist.</summary>
        public void AddObjective(ObjectiveDefinition objective)
        {
            if (objective == null)
                throw new ArgumentNullException(nameof(objective));

            ObjectiveList.Add(objective);
        }

        /// <summary>Replaces the whole checklist. A null sequence clears it.</summary>
        public void SetObjectives(IEnumerable<ObjectiveDefinition> objectives)
        {
            var list = ObjectiveList;
            list.Clear();

            if (objectives == null)
                return;

            foreach (var objective in objectives)
                list.Add(objective);
        }

        /// <summary>Builds the runtime checklist a session works through.</summary>
        public ObjectiveTracker CreateObjectiveTracker() =>
            new ObjectiveTracker(ObjectiveList.Where(o => o != null).Select(o => o.ToObjective()));

        /// <summary>
        /// Lists everything that would stop this definition being playable, in words an instructor
        /// can act on. An empty list means the definition is usable.
        ///
        /// Reporting every problem at once matters: an instructor editing a scenario should see
        /// the whole list rather than fixing one fault only to be told about the next.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(m_ScenarioId))
                problems.Add("The scenario has no id. Results are filed under it, so it has to be set.");

            if (m_TimeLimitSeconds < 0f)
                problems.Add("The time limit is negative. Use 0 for an untimed scenario.");

            var objectives = ObjectiveList;
            if (objectives.Count == 0)
            {
                problems.Add("The scenario has no objectives, so there is nothing for the user to finish.");
                return problems;
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < objectives.Count; index++)
            {
                var objective = objectives[index];
                var position = index + 1;

                if (objective == null)
                {
                    problems.Add($"Objective {position} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(objective.Id))
                    problems.Add($"Objective {position} has no id, so nothing in the scene could complete it.");
                else if (!seenIds.Add(objective.Id))
                    problems.Add($"Objective {position} repeats the id '{objective.Id}'. Ids have to be unique within a scenario.");

                if (string.IsNullOrWhiteSpace(objective.Title))
                    problems.Add($"Objective {position} has no title, so the checklist would show a blank line.");
            }

            return problems;
        }

        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>
        /// Restores a definition, falling back to an empty one when the text is unusable. The
        /// fallback deliberately fails <see cref="Validate"/>, so a corrupt file is reported to
        /// the instructor rather than silently loaded as a playable scenario.
        /// </summary>
        public static ScenarioDefinition FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ScenarioDefinition();

            try
            {
                return JsonUtility.FromJson<ScenarioDefinition>(json) ?? new ScenarioDefinition();
            }
            catch (Exception)
            {
                // A corrupt scenario file must never stop the rest of the catalogue loading.
                return new ScenarioDefinition();
            }
        }
    }
}
