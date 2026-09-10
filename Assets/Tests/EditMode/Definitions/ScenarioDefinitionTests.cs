using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRSim.Scenario;
using VRSim.Scenario.Definitions;

namespace VRSim.Tests
{
    public class ScenarioDefinitionTests
    {
        /// <summary>The smallest definition that is actually playable, for the tests to break.</summary>
        static ScenarioDefinition NewDefinition()
        {
            var definition = new ScenarioDefinition
            {
                ScenarioId = "test_scenario_01",
                DisplayName = "Test Scenario",
                Summary = "A scenario used by the tests.",
                BriefingIntro = "You are in a room. Something has happened. Deal with it.",
                TimeLimitSeconds = 120f,
                Difficulty = ScenarioDifficulty.Standard,
            };

            definition.SetObjectives(new[]
            {
                new ObjectiveDefinition("first", "Do the first thing", "It is over there."),
                new ObjectiveDefinition("second", "Do the second thing", "It is over here."),
            });

            return definition;
        }

        static bool Mentions(IReadOnlyList<string> problems, string fragment) =>
            problems.Any(problem => problem.Contains(fragment));

        [Test]
        public void AWellFormedDefinitionHasNoProblems()
        {
            var definition = NewDefinition();

            Assert.AreEqual(0, definition.Validate().Count,
                string.Join(" | ", definition.Validate()));
            Assert.IsTrue(definition.IsValid);
        }

        [Test]
        public void AnUntimedScenarioIsValid()
        {
            var definition = NewDefinition();
            definition.TimeLimitSeconds = 0f;

            Assert.IsTrue(definition.IsValid, "zero means untimed, not misconfigured");
        }

        [Test]
        public void RejectsADefinitionWithAnEmptyId()
        {
            var definition = NewDefinition();
            definition.ScenarioId = "   ";

            var problems = definition.Validate();

            Assert.IsTrue(Mentions(problems, "The scenario has no id"));
            Assert.IsFalse(definition.IsValid);
        }

        [Test]
        public void RejectsADefinitionWithNoObjectives()
        {
            var definition = NewDefinition();
            definition.SetObjectives(null);

            Assert.IsTrue(Mentions(definition.Validate(), "no objectives"));
        }

        [Test]
        public void RejectsDuplicateObjectiveIds()
        {
            var definition = NewDefinition();
            definition.AddObjective(new ObjectiveDefinition("first", "Do the first thing again", "Again."));

            Assert.IsTrue(Mentions(definition.Validate(), "repeats the id"));
        }

        [Test]
        public void RejectsANegativeTimeLimit()
        {
            var definition = NewDefinition();
            definition.TimeLimitSeconds = -1f;

            Assert.IsTrue(Mentions(definition.Validate(), "time limit is negative"));
        }

        [Test]
        public void RejectsAnObjectiveWithNoTitle()
        {
            var definition = NewDefinition();
            definition.AddObjective(new ObjectiveDefinition("third", "  ", "It is somewhere."));

            Assert.IsTrue(Mentions(definition.Validate(), "has no title"));
        }

        [Test]
        public void RejectsAnObjectiveWithNoId()
        {
            var definition = NewDefinition();
            definition.AddObjective(new ObjectiveDefinition(null, "Do the third thing", "Over there."));

            Assert.IsTrue(Mentions(definition.Validate(), "has no id"));
        }

        [Test]
        public void ReportsEveryProblemAtOnceRatherThanTheFirst()
        {
            var definition = new ScenarioDefinition { TimeLimitSeconds = -5f };
            definition.SetObjectives(new[]
            {
                new ObjectiveDefinition("only", string.Empty, "Nowhere in particular."),
            });

            var problems = definition.Validate();

            Assert.IsTrue(Mentions(problems, "The scenario has no id"));
            Assert.IsTrue(Mentions(problems, "time limit is negative"));
            Assert.IsTrue(Mentions(problems, "has no title"));
            Assert.AreEqual(3, problems.Count, string.Join(" | ", problems));
        }

        [Test]
        public void ProblemsNameTheObjectiveByItsPositionInTheList()
        {
            var definition = NewDefinition();
            definition.AddObjective(new ObjectiveDefinition("third", null, "Over there."));

            Assert.IsTrue(Mentions(definition.Validate(), "Objective 3"),
                "an instructor needs to know which row to fix");
        }

        [Test]
        public void SurvivesAJsonRoundTrip()
        {
            var definition = NewDefinition();
            definition.Difficulty = ScenarioDifficulty.Challenging;

            var restored = ScenarioDefinition.FromJson(definition.ToJson());

            Assert.AreEqual("test_scenario_01", restored.ScenarioId);
            Assert.AreEqual("Test Scenario", restored.DisplayName);
            Assert.AreEqual("A scenario used by the tests.", restored.Summary);
            Assert.AreEqual(definition.BriefingIntro, restored.BriefingIntro);
            Assert.AreEqual(120f, restored.TimeLimitSeconds, 0.0001f);
            Assert.AreEqual(ScenarioDifficulty.Challenging, restored.Difficulty);
            Assert.IsTrue(restored.IsValid);
        }

        [Test]
        public void ObjectivesKeepTheirOrderAndTextAcrossAJsonRoundTrip()
        {
            var restored = ScenarioDefinition.FromJson(NewDefinition().ToJson());

            CollectionAssert.AreEqual(
                new[] { "first", "second" }, restored.Objectives.Select(o => o.Id).ToArray());
            Assert.AreEqual("Do the first thing", restored.Objectives[0].Title);
            Assert.AreEqual("It is over here.", restored.Objectives[1].Hint);
        }

        [Test]
        public void RestoringFromRubbishJsonFallsBackToADefault()
        {
            var restored = ScenarioDefinition.FromJson("not json at all");

            Assert.IsNotNull(restored, "a corrupt file must not throw at the caller");
            Assert.IsFalse(restored.IsValid, "the fallback is reported, not quietly played");
        }

        [Test]
        public void RestoringFromEmptyTextFallsBackToADefault()
        {
            Assert.IsNotNull(ScenarioDefinition.FromJson(null));
            Assert.IsNotNull(ScenarioDefinition.FromJson("   "));
            Assert.IsFalse(ScenarioDefinition.FromJson(string.Empty).IsValid);
        }

        [Test]
        public void RestoringJsonWithNoObjectivesStillHasAnEmptyList()
        {
            var restored = ScenarioDefinition.FromJson("{}");

            Assert.IsNotNull(restored.Objectives, "the checklist is never null, only empty");
            Assert.AreEqual(0, restored.Objectives.Count);
            Assert.DoesNotThrow(() => restored.Validate());
        }

        [Test]
        public void ANewDefinitionStartsEmptyAndInvalid()
        {
            var definition = new ScenarioDefinition();

            Assert.AreEqual(0, definition.Objectives.Count);
            Assert.IsFalse(definition.IsValid);
        }

        [Test]
        public void BuildsARuntimeTrackerFromTheObjectives()
        {
            var tracker = NewDefinition().CreateObjectiveTracker();

            CollectionAssert.AreEqual(
                new[] { "first", "second" }, tracker.Objectives.Select(o => o.Id).ToArray());
            Assert.AreEqual("0/2", tracker.Progress);

            tracker.Complete("first");

            Assert.AreEqual("second", tracker.Current.Id);
        }

        [Test]
        public void TheTrackerIsANewInstanceEachTimeSoRunsCannotShareProgress()
        {
            var definition = NewDefinition();
            var first = definition.CreateObjectiveTracker();

            first.Complete("first");

            Assert.AreEqual(0, definition.CreateObjectiveTracker().CompletedCount);
        }

        [Test]
        public void SettingObjectivesReplacesTheWholeChecklist()
        {
            var definition = NewDefinition();

            definition.SetObjectives(new[] { new ObjectiveDefinition("only", "Do it", "Here.") });

            CollectionAssert.AreEqual(
                new[] { "only" }, definition.Objectives.Select(o => o.Id).ToArray());
        }

        [Test]
        public void AddingANullObjectiveIsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => NewDefinition().AddObjective(null));
        }

        [Test]
        public void AnObjectiveDefinitionConvertsToItsRuntimeObjective()
        {
            var objective = new ObjectiveDefinition("id", "Title", "Hint").ToObjective();

            Assert.AreEqual("id", objective.Id);
            Assert.AreEqual("Title", objective.Title);
            Assert.AreEqual("Hint", objective.Hint);
            Assert.IsFalse(objective.IsComplete, "a definition carries no per-run state");
        }
    }
}
