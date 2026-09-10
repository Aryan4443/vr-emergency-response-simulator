using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRSim.Scenario;
using VRSim.Scenario.Definitions;

namespace VRSim.Tests
{
    public class ScenarioCatalogueTests
    {
        static IReadOnlyList<ScenarioDefinition> BuiltIns() => ScenarioCatalogue.CreateBuiltIns();

        static ScenarioDefinition NewDefinition(string id)
        {
            var definition = new ScenarioDefinition
            {
                ScenarioId = id,
                DisplayName = "Instructor Scenario",
                Summary = "Authored in the instructor mode.",
                BriefingIntro = "You are in a room. Something has happened.",
            };

            definition.AddObjective(new ObjectiveDefinition("only", "Do the thing", "Over there."));
            return definition;
        }

        // ------------------------------------------------------------------ built-in drills

        [Test]
        public void ShipsTheThreeBuiltInDrills()
        {
            CollectionAssert.AreEqual(
                new[] { "fire_evacuation_01", "earthquake_01", "chemical_spill_01" },
                BuiltIns().Select(d => d.ScenarioId).ToArray());
        }

        [Test]
        public void EveryBuiltInDefinitionIsValid()
        {
            foreach (var definition in BuiltIns())
            {
                var problems = definition.Validate();
                Assert.AreEqual(0, problems.Count,
                    $"{definition.ScenarioId}: {string.Join(" | ", problems)}");
            }
        }

        [Test]
        public void EveryBuiltInHasABriefingAndSomethingToShowOnTheLevelSelect()
        {
            foreach (var definition in BuiltIns())
            {
                Assert.IsNotEmpty(definition.DisplayName, $"{definition.ScenarioId} has no display name");
                Assert.IsNotEmpty(definition.Summary, $"{definition.ScenarioId} has no summary");
                Assert.IsNotEmpty(definition.BriefingIntro, $"{definition.ScenarioId} has no briefing");
            }
        }

        [Test]
        public void EveryBuiltInHasAtLeastFourObjectives()
        {
            foreach (var definition in BuiltIns())
                Assert.GreaterOrEqual(definition.Objectives.Count, 4,
                    $"{definition.ScenarioId} is too thin to be a drill");
        }

        [Test]
        public void ObjectiveIdsAreUniqueWithinEachBuiltIn()
        {
            foreach (var definition in BuiltIns())
            {
                var ids = definition.Objectives.Select(o => o.Id).ToArray();
                CollectionAssert.AllItemsAreUnique(ids, $"{definition.ScenarioId} repeats an objective id");
            }
        }

        [Test]
        public void EveryBuiltInObjectiveHasAnInstructionAndAHint()
        {
            foreach (var definition in BuiltIns())
            foreach (var objective in definition.Objectives)
            {
                Assert.IsNotEmpty(objective.Title, $"{definition.ScenarioId}/{objective.Id} has no title");
                Assert.IsNotEmpty(objective.Hint, $"{definition.ScenarioId}/{objective.Id} has no hint");
            }
        }

        [Test]
        public void BuiltInScenarioIdsAreUnique()
        {
            CollectionAssert.AllItemsAreUnique(BuiltIns().Select(d => d.ScenarioId).ToArray());
        }

        [Test]
        public void EveryBuiltInGivesAPositiveTimeLimitAndADifficulty()
        {
            foreach (var definition in BuiltIns())
            {
                Assert.Greater(definition.TimeLimitSeconds, 0f, $"{definition.ScenarioId} is untimed");
                Assert.IsTrue(System.Enum.IsDefined(typeof(ScenarioDifficulty), definition.Difficulty));
            }
        }

        [Test]
        public void EveryBuiltInSurvivesAJsonRoundTrip()
        {
            foreach (var definition in BuiltIns())
            {
                var restored = ScenarioDefinition.FromJson(definition.ToJson());

                Assert.AreEqual(definition.ScenarioId, restored.ScenarioId);
                Assert.AreEqual(definition.Difficulty, restored.Difficulty);
                Assert.AreEqual(definition.TimeLimitSeconds, restored.TimeLimitSeconds, 0.0001f);
                CollectionAssert.AreEqual(
                    definition.Objectives.Select(o => o.Id).ToArray(),
                    restored.Objectives.Select(o => o.Id).ToArray());
                Assert.IsTrue(restored.IsValid, $"{definition.ScenarioId} did not survive the round trip");
            }
        }

        [Test]
        public void TheFireDrillMatchesTheChecklistTheSessionAlreadyUses()
        {
            var hardcoded = ObjectiveTracker.CreateFireEvacuationObjectives().Objectives
                .Select(o => o.Id).ToArray();

            CollectionAssert.AreEqual(hardcoded,
                ScenarioCatalogue.FireEvacuation().Objectives.Select(o => o.Id).ToArray(),
                "the definition has to drive the existing scene unchanged");
        }

        [Test]
        public void EachCallReturnsFreshBuiltInsSoEditingOneCannotAffectAnother()
        {
            var first = ScenarioCatalogue.FireEvacuation();
            first.DisplayName = "Edited";

            Assert.AreEqual("Fire Evacuation", ScenarioCatalogue.FireEvacuation().DisplayName);
        }

        // -------------------------------------------------------------------- the catalogue

        [Test]
        public void ACatalogueStartsWithTheBuiltInScenarios()
        {
            var catalogue = new ScenarioCatalogue();

            Assert.AreEqual(3, catalogue.Count);
            CollectionAssert.AreEqual(
                new[] { "fire_evacuation_01", "earthquake_01", "chemical_spill_01" },
                catalogue.Ids.ToArray());
        }

        [Test]
        public void ACatalogueCanBeStartedEmptyForInstructorAuthoredCourses()
        {
            var catalogue = new ScenarioCatalogue(includeBuiltIns: false);

            Assert.AreEqual(0, catalogue.Count);
            Assert.AreEqual(0, catalogue.All.Count);
            Assert.AreEqual(0, catalogue.Ids.Count);
        }

        [Test]
        public void TryGetFindsAScenarioById()
        {
            var catalogue = new ScenarioCatalogue();

            Assert.IsTrue(catalogue.TryGet("earthquake_01", out var definition));
            Assert.AreEqual("Earthquake", definition.DisplayName);
        }

        [Test]
        public void TryGetMissesAnUnknownId()
        {
            var catalogue = new ScenarioCatalogue();

            Assert.IsFalse(catalogue.TryGet("volcano_01", out var definition));
            Assert.IsNull(definition);
        }

        [Test]
        public void TryGetMissesOnAnEmptyOrNullId()
        {
            var catalogue = new ScenarioCatalogue();

            Assert.IsFalse(catalogue.TryGet(null, out _));
            Assert.IsFalse(catalogue.TryGet("   ", out _));
        }

        [Test]
        public void RegisterAcceptsAValidDefinition()
        {
            var catalogue = new ScenarioCatalogue(includeBuiltIns: false);

            Assert.IsTrue(catalogue.Register(NewDefinition("medical_emergency_01"), out var problems));
            Assert.AreEqual(0, problems.Count);
            Assert.IsTrue(catalogue.TryGet("medical_emergency_01", out _));
            Assert.AreEqual(1, catalogue.Count);
        }

        [Test]
        public void RegisterRejectsAnInvalidDefinitionAndSaysWhy()
        {
            var catalogue = new ScenarioCatalogue(includeBuiltIns: false);
            var broken = new ScenarioDefinition { DisplayName = "Half finished" };

            Assert.IsFalse(catalogue.Register(broken, out var problems));
            Assert.Greater(problems.Count, 0, "a rejection has to explain itself");
            Assert.AreEqual(0, catalogue.Count, "a broken scenario must not reach a headset");
        }

        [Test]
        public void RegisterRejectsAScenarioIdThatIsAlreadyTaken()
        {
            var catalogue = new ScenarioCatalogue();

            Assert.IsFalse(catalogue.Register(NewDefinition("fire_evacuation_01"), out var problems));
            Assert.IsTrue(problems.Any(p => p.Contains("already in the catalogue")));
            Assert.AreEqual(3, catalogue.Count);
        }

        [Test]
        public void RegisterRejectsNothingAtAll()
        {
            var catalogue = new ScenarioCatalogue(includeBuiltIns: false);

            Assert.IsFalse(catalogue.Register(null, out var problems));
            Assert.Greater(problems.Count, 0);
            Assert.AreEqual(0, catalogue.Count);
        }

        [Test]
        public void RegisteredScenariosAppearAfterTheBuiltInOnes()
        {
            var catalogue = new ScenarioCatalogue();
            catalogue.Register(NewDefinition("medical_emergency_01"));

            CollectionAssert.AreEqual(
                new[] { "fire_evacuation_01", "earthquake_01", "chemical_spill_01", "medical_emergency_01" },
                catalogue.Ids.ToArray());
        }

        [Test]
        public void TheShortRegisterOverloadReportsTheSameDecision()
        {
            var catalogue = new ScenarioCatalogue(includeBuiltIns: false);

            Assert.IsTrue(catalogue.Register(NewDefinition("medical_emergency_01")));
            Assert.IsFalse(catalogue.Register(NewDefinition("medical_emergency_01")));
        }
    }
}
