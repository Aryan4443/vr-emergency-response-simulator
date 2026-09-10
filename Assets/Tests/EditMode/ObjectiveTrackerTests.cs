using System.Linq;
using NUnit.Framework;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class ObjectiveTrackerTests
    {
        static ObjectiveTracker NewTracker() => ObjectiveTracker.CreateFireEvacuationObjectives();

        [Test]
        public void TheFireDrillHasFourObjectivesInOrder()
        {
            var tracker = NewTracker();

            var ids = tracker.Objectives.Select(o => o.Id).ToArray();

            CollectionAssert.AreEqual(
                new[] { "read_map", "raise_alarm", "avoid_hazard", "reach_exit" }, ids);
        }

        [Test]
        public void EveryObjectiveStartsIncomplete()
        {
            var tracker = NewTracker();

            Assert.IsTrue(tracker.Objectives.All(o => !o.IsComplete));
            Assert.AreEqual(0, tracker.CompletedCount);
            Assert.IsFalse(tracker.AllComplete);
        }

        [Test]
        public void TheFirstIncompleteObjectiveIsTheCurrentOne()
        {
            var tracker = NewTracker();

            Assert.AreEqual("read_map", tracker.Current.Id);

            tracker.Complete("read_map");

            Assert.AreEqual("raise_alarm", tracker.Current.Id);
        }

        [Test]
        public void CompletingOutOfOrderIsAllowed()
        {
            var tracker = NewTracker();

            tracker.Complete("reach_exit");

            Assert.IsTrue(tracker.Objectives.Single(o => o.Id == "reach_exit").IsComplete);
            Assert.AreEqual("read_map", tracker.Current.Id, "the next unfinished objective is still first");
        }

        [Test]
        public void CompletingTheSameObjectiveTwiceCountsOnce()
        {
            var tracker = NewTracker();

            tracker.Complete("read_map");
            tracker.Complete("read_map");

            Assert.AreEqual(1, tracker.CompletedCount);
        }

        [Test]
        public void AnUnknownObjectiveIsIgnored()
        {
            var tracker = NewTracker();

            Assert.DoesNotThrow(() => tracker.Complete("not_an_objective"));
            Assert.AreEqual(0, tracker.CompletedCount);
        }

        [Test]
        public void FinishingEveryObjectiveReportsAllComplete()
        {
            var tracker = NewTracker();

            foreach (var objective in tracker.Objectives.ToList())
                tracker.Complete(objective.Id);

            Assert.IsTrue(tracker.AllComplete);
            Assert.AreEqual(4, tracker.CompletedCount);
            Assert.IsNull(tracker.Current, "there is no current objective once they are all done");
        }

        [Test]
        public void RaisesChangedWhenAnObjectiveIsCompleted()
        {
            var tracker = NewTracker();
            var changes = 0;
            tracker.Changed += () => changes++;

            tracker.Complete("read_map");
            tracker.Complete("read_map");

            Assert.AreEqual(1, changes, "only a real change notifies");
        }

        [Test]
        public void ResetClearsEveryObjective()
        {
            var tracker = NewTracker();
            tracker.Complete("read_map");

            tracker.Reset();

            Assert.AreEqual(0, tracker.CompletedCount);
            Assert.AreEqual("read_map", tracker.Current.Id);
        }

        [Test]
        public void EachObjectiveHasAnInstructionAndAHint()
        {
            var tracker = NewTracker();

            foreach (var objective in tracker.Objectives)
            {
                Assert.IsNotEmpty(objective.Title, $"{objective.Id} has no title");
                Assert.IsNotEmpty(objective.Hint, $"{objective.Id} has no hint");
            }
        }

        [Test]
        public void ProgressReadsAsCompletedOverTotal()
        {
            var tracker = NewTracker();
            tracker.Complete("read_map");

            Assert.AreEqual("1/4", tracker.Progress);
        }
    }
}
