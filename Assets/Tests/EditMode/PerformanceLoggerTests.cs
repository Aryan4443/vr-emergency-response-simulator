using NUnit.Framework;
using VRSim.Evaluation;

namespace VRSim.Tests
{
    public class PerformanceLoggerTests
    {
        [Test]
        public void StartsWithNoEvents()
        {
            var logger = new PerformanceLogger(new ScenarioTimer());

            Assert.AreEqual(0, logger.Events.Count);
        }

        [Test]
        public void StampsEachEventWithTheElapsedScenarioTime()
        {
            var timer = new ScenarioTimer();
            var logger = new PerformanceLogger(timer);
            timer.Start();
            timer.Tick(12.5f);

            logger.Record("alarm_activated", "FireAlarm_A", successful: true);

            Assert.AreEqual(1, logger.Events.Count);
            Assert.AreEqual("alarm_activated", logger.Events[0].actionType);
            Assert.AreEqual("FireAlarm_A", logger.Events[0].objectId);
            Assert.AreEqual(12.5f, logger.Events[0].timestampSeconds, 0.0001f);
            Assert.IsTrue(logger.Events[0].successful);
        }

        [Test]
        public void KeepsEventsInTheOrderTheyHappened()
        {
            var timer = new ScenarioTimer();
            var logger = new PerformanceLogger(timer);
            timer.Start();

            timer.Tick(1f);
            logger.Record("map_viewed", "EvacuationMap", successful: true);
            timer.Tick(1f);
            logger.Record("hazard_entered", "HazardZone_A", successful: false);

            Assert.AreEqual("map_viewed", logger.Events[0].actionType);
            Assert.AreEqual("hazard_entered", logger.Events[1].actionType);
        }

        [Test]
        public void BuildsAResultFromTheTimerTheScoreAndTheLog()
        {
            var timer = new ScenarioTimer();
            var board = new ScoreBoard();
            var logger = new PerformanceLogger(timer);

            timer.Start();
            timer.Tick(92.4f);
            board.Record(ScoreEvent.AlarmActivated);
            board.Record(ScoreEvent.UnsafeAreaEntered);
            board.Record(ScoreEvent.UnsafeAreaEntered);
            board.Record(ScoreEvent.WrongExitSelected);
            logger.Record("alarm_activated", "FireAlarm_A", successful: true);

            var result = logger.BuildResult("fire_evacuation_01", board, completed: true, selectedExit: "north_exit");

            Assert.AreEqual("fire_evacuation_01", result.scenarioId);
            Assert.IsTrue(result.completed);
            Assert.AreEqual(92.4f, result.completionTimeSeconds, 0.0001f);
            Assert.AreEqual(2, result.unsafeZoneEntries);
            Assert.AreEqual("north_exit", result.selectedExit);
            Assert.AreEqual(board.Total, result.score);
            Assert.AreEqual(1, result.events.Count);
        }

        [Test]
        public void CountsWrongExitsAndCriticalActionsAsIncorrectActions()
        {
            var board = new ScoreBoard();
            var logger = new PerformanceLogger(new ScenarioTimer());

            board.Record(ScoreEvent.WrongExitSelected);
            board.Record(ScoreEvent.CriticalUnsafeAction);
            board.Record(ScoreEvent.CriticalUnsafeAction);

            var result = logger.BuildResult("fire_evacuation_01", board, completed: false, selectedExit: null);

            Assert.AreEqual(3, result.incorrectActions);
        }

        [Test]
        public void ResetClearsTheLog()
        {
            var logger = new PerformanceLogger(new ScenarioTimer());
            logger.Record("map_viewed", "EvacuationMap", successful: true);

            logger.Reset();

            Assert.AreEqual(0, logger.Events.Count);
        }
    }
}
