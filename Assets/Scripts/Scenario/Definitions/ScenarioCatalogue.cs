using System.Collections.Generic;
using System.Linq;

namespace VRSim.Scenario.Definitions
{
    /// <summary>
    /// The set of scenarios the build can offer, held in memory.
    ///
    /// Section 20 asks for more drills than the fire evacuation and for an instructor mode that
    /// configures them. Both are served by one collection: the built-in drills are seeded from
    /// code so a fresh install always has something to play, and anything an instructor authors is
    /// validated before it joins them, so a half-finished scenario can never reach a headset.
    /// </summary>
    public class ScenarioCatalogue
    {
        readonly List<ScenarioDefinition> m_Definitions = new List<ScenarioDefinition>();

        /// <param name="includeBuiltIns">
        /// Seeds the shipped drills. Pass false when an instructor is assembling a course from
        /// their own scenarios and the built-in ones would only get in the way.
        /// </param>
        public ScenarioCatalogue(bool includeBuiltIns = true)
        {
            if (!includeBuiltIns)
                return;

            m_Definitions.AddRange(CreateBuiltIns());
        }

        /// <summary>Every scenario currently in the catalogue, in the order it was added.</summary>
        public IReadOnlyList<ScenarioDefinition> All => m_Definitions;

        /// <summary>The ids in the same order as <see cref="All"/>, for menus and result filters.</summary>
        public IReadOnlyList<string> Ids => m_Definitions.Select(d => d.ScenarioId).ToList();

        public int Count => m_Definitions.Count;

        /// <summary>
        /// Fresh copies of the shipped drills. New instances each call, so an instructor editing
        /// one catalogue cannot change what another catalogue starts from.
        /// </summary>
        public static IReadOnlyList<ScenarioDefinition> CreateBuiltIns() => new List<ScenarioDefinition>
        {
            FireEvacuation(),
            Earthquake(),
            ChemicalSpill(),
        };

        /// <summary>Finds a scenario by id. Unknown, empty and null ids all miss.</summary>
        public bool TryGet(string scenarioId, out ScenarioDefinition definition)
        {
            definition = string.IsNullOrWhiteSpace(scenarioId)
                ? null
                : m_Definitions.FirstOrDefault(d => d.ScenarioId == scenarioId);

            return definition != null;
        }

        /// <summary>
        /// Adds a scenario after checking it. Returns false and the reasons when it is unusable,
        /// which is what the instructor mode shows next to the failing field.
        /// </summary>
        public bool Register(ScenarioDefinition definition, out IReadOnlyList<string> problems)
        {
            if (definition == null)
            {
                problems = new[] { "There is no scenario definition to register." };
                return false;
            }

            var found = definition.Validate().ToList();

            // Two scenarios sharing an id would make stored results impossible to tell apart.
            if (TryGet(definition.ScenarioId, out _))
                found.Add($"A scenario with the id '{definition.ScenarioId}' is already in the catalogue.");

            problems = found;

            if (found.Count > 0)
                return false;

            m_Definitions.Add(definition);
            return true;
        }

        /// <summary>For callers that only need to know whether the scenario was accepted.</summary>
        public bool Register(ScenarioDefinition definition) => Register(definition, out _);

        /// <summary>
        /// The drill the prototype already ships. The objective ids match the ones
        /// <see cref="ObjectiveTracker.CreateFireEvacuationObjectives"/> hardcodes, so the existing
        /// scene keeps working while it is moved over to definitions.
        /// </summary>
        public static ScenarioDefinition FireEvacuation()
        {
            var definition = new ScenarioDefinition
            {
                ScenarioId = "fire_evacuation_01",
                DisplayName = "Fire Evacuation",
                Summary =
                    "A fire breaks out while you are in a classroom. Read the evacuation map, raise the " +
                    "alarm, keep out of the smoke and leave by the safe exit.",
                BriefingIntro =
                    "You are in a classroom. A fire has started in the building. Get out safely.",
                TimeLimitSeconds = 180f,
                Difficulty = ScenarioDifficulty.Introductory,
            };

            definition.SetObjectives(new[]
            {
                new ObjectiveDefinition("read_map", "Read the evacuation map",
                    "The white board on the right-hand wall of the hallway."),
                new ObjectiveDefinition("raise_alarm", "Raise the fire alarm",
                    "The red box on the left-hand wall, opposite the map."),
                new ObjectiveDefinition("avoid_hazard", "Keep out of the smoke",
                    "The south end of the hallway is filling with smoke. Do not walk into it."),
                new ObjectiveDefinition("reach_exit", "Leave by the safe exit",
                    "Follow the green exit sign at the north end of the hallway."),
            });

            return definition;
        }

        /// <summary>
        /// The drop, cover and hold drill. Ordered the way the advice actually runs: the first
        /// seconds are about not being hit, and evacuation only starts once the ground is still.
        /// </summary>
        public static ScenarioDefinition Earthquake()
        {
            var definition = new ScenarioDefinition
            {
                ScenarioId = "earthquake_01",
                DisplayName = "Earthquake",
                Summary =
                    "The ground starts shaking during a lesson. Take cover until it stops, then leave " +
                    "the building by the stairs and reach the assembly point.",
                BriefingIntro =
                    "You are in a classroom. The floor has started to shake. Protect yourself first, " +
                    "then get out of the building.",
                TimeLimitSeconds = 240f,
                Difficulty = ScenarioDifficulty.Standard,
            };

            definition.SetObjectives(new[]
            {
                new ObjectiveDefinition("drop_cover_hold", "Drop, cover and hold on",
                    "Get under the nearest desk, face away from the glass and hold on to a leg so the " +
                    "desk stays over you while it moves."),
                new ObjectiveDefinition("wait_for_shaking", "Stay under cover until the shaking stops",
                    "Count it out where you are. Standing up mid-tremor is what knocks people down and " +
                    "puts them under falling ceiling tiles."),
                new ObjectiveDefinition("avoid_falling_hazards", "Keep clear of the windows and tall furniture",
                    "The glazed wall and the loaded bookcases are the first things to come down. Take the " +
                    "route along the inner wall, not the one past them."),
                new ObjectiveDefinition("use_stairs", "Leave by the stairs, not the lift",
                    "The stairwell is at the north end. Lifts lose power and jam in the shaft after a quake."),
                new ObjectiveDefinition("reach_assembly", "Reach the open assembly point",
                    "Cross to the open ground away from the walls. Aftershocks bring down masonry, glass " +
                    "and signage on to the pavement beside the building."),
            });

            return definition;
        }

        /// <summary>
        /// The laboratory spill drill. The order is deliberate: knowing what was spilled decides
        /// everything after it, and decontamination comes before evacuation because carrying the
        /// chemical out on your skin makes the injury worse the longer it is left.
        /// </summary>
        public static ScenarioDefinition ChemicalSpill()
        {
            var definition = new ScenarioDefinition
            {
                ScenarioId = "chemical_spill_01",
                DisplayName = "Chemical Spill",
                Summary =
                    "A container of solvent goes over in a teaching laboratory. Work out what it is, keep " +
                    "out of it, warn everyone, wash off anything that reached you and leave upwind.",
                BriefingIntro =
                    "You are in a teaching laboratory. A container has been knocked over and is spreading " +
                    "across the floor. Keep yourself safe and clear the room.",
                TimeLimitSeconds = 300f,
                Difficulty = ScenarioDifficulty.Challenging,
            };

            definition.SetObjectives(new[]
            {
                new ObjectiveDefinition("identify_spill", "Identify what has been spilled",
                    "Read the label on the container and the hazard placard by the fume cupboard. What it " +
                    "is decides whether this is a rinse, a spill kit or a full evacuation."),
                new ObjectiveDefinition("avoid_contact", "Keep out of the liquid and its vapour",
                    "The pool is spreading towards the middle of the room. Do not walk through it, do not " +
                    "step over it, and do not lean across it to reach the bench."),
                new ObjectiveDefinition("raise_alarm", "Warn everyone and raise the alarm",
                    "The red call point by the laboratory door. It also warns the prep room next door, " +
                    "where nobody can see what has happened in here."),
                new ObjectiveDefinition("use_eyewash", "Wash off anything that reached you",
                    "The emergency shower and eyewash station are by the sink. Splashes on skin or in the " +
                    "eyes need rinsing for a full fifteen minutes, starting now rather than outside."),
                new ObjectiveDefinition("evacuate_upwind", "Leave upwind of the spill",
                    "Take the door on the windward side and keep the draught behind you, so the vapour is " +
                    "carried away from your route out rather than along it."),
            });

            return definition;
        }
    }
}
