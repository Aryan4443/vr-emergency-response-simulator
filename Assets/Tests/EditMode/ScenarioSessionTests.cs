using System.Linq;
using NUnit.Framework;
using VRSim.Evaluation;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class ScenarioSessionTests
    {
        static ScenarioSession NewSession(float timeLimit = 180f) =>
            new ScenarioSession("fire_evacuation_01", timeLimit);

        static ScenarioSession RunningSession(float timeLimit = 180f)
        {
            var session = NewSession(timeLimit);
            session.BeginBriefing();
            session.StartScenario();
            return session;
        }

        [Test]
        public void StartsReadyWithNoScoreAndNoTime()
        {
            var session = NewSession();

            Assert.AreEqual(ScenarioState.Ready, session.State);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0f, session.ElapsedSeconds);
        }

        [Test]
        public void BriefingThenStartRunsTheClock()
        {
            var session = NewSession();

            session.BeginBriefing();
            Assert.AreEqual(ScenarioState.Briefing, session.State);

            session.StartScenario();
            session.Tick(2f);

            Assert.AreEqual(ScenarioState.Active, session.State);
            Assert.AreEqual(2f, session.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void TheClockDoesNotRunDuringTheBriefing()
        {
            var session = NewSession();
            session.BeginBriefing();

            session.Tick(5f);

            Assert.AreEqual(0f, session.ElapsedSeconds);
        }

        [Test]
        public void ActivatingTheAlarmScoresAndLogsIt()
        {
            var session = RunningSession();

            session.ActivateAlarm("FireAlarm_A");

            Assert.AreEqual(ScoreBoard.ValueOf(ScoreEvent.AlarmActivated), session.Score);
            Assert.AreEqual("alarm_activated", session.Events.Single().actionType);
            Assert.AreEqual("FireAlarm_A", session.Events.Single().objectId);
        }

        [Test]
        public void ViewingTheMapScoresAndLogsIt()
        {
            var session = RunningSession();

            session.ViewMap("EvacuationMap_A");

            Assert.AreEqual(ScoreBoard.ValueOf(ScoreEvent.EvacuationMapViewed), session.Score);
            Assert.AreEqual("map_viewed", session.Events.Single().actionType);
        }

        [Test]
        public void OpeningADoorIsLoggedButDoesNotScore()
        {
            var session = RunningSession();

            session.OpenDoor("Door_Hallway");

            Assert.AreEqual(0, session.Score);
            Assert.AreEqual("door_opened", session.Events.Single().actionType);
        }

        [Test]
        public void EnteringAHazardWarnsPenalisesAndLogs()
        {
            var session = RunningSession();

            session.EnterHazard("HazardZone_A");

            Assert.AreEqual(ScenarioState.Warning, session.State);
            Assert.AreEqual(ScoreBoard.ValueOf(ScoreEvent.UnsafeAreaEntered), session.Score);
            Assert.AreEqual("hazard_entered", session.Events.Single().actionType);
            Assert.IsFalse(session.Events.Single().successful);
        }

        [Test]
        public void LeavingAHazardReturnsToActive()
        {
            var session = RunningSession();
            session.EnterHazard("HazardZone_A");

            session.LeaveHazard("HazardZone_A");

            Assert.AreEqual(ScenarioState.Active, session.State);
        }

        [Test]
        public void EveryHazardEntryIsPenalisedSeparately()
        {
            var session = RunningSession();

            session.EnterHazard("HazardZone_A");
            session.LeaveHazard("HazardZone_A");
            session.EnterHazard("HazardZone_A");

            Assert.AreEqual(2 * ScoreBoard.ValueOf(ScoreEvent.UnsafeAreaEntered), session.Score);
        }

        [Test]
        public void ReachingTheCorrectExitCompletesTheScenario()
        {
            var session = RunningSession();
            session.Tick(30f);

            session.ReachExit("north_exit", isCorrect: true);

            Assert.AreEqual(ScenarioState.Completed, session.State);
            Assert.AreEqual("safe_exit_reached", session.Events.Single().actionType);
            Assert.AreEqual(
                ScoreBoard.ValueOf(ScoreEvent.CorrectSafeExit) + ScoreBoard.ValueOf(ScoreEvent.SuccessfulCompletion),
                session.Score);
        }

        [Test]
        public void TheClockStopsOnceTheScenarioIsCompleted()
        {
            var session = RunningSession();
            session.Tick(30f);
            session.ReachExit("north_exit", isCorrect: true);

            session.Tick(10f);

            Assert.AreEqual(30f, session.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void ReachingTheWrongExitPenalisesAndKeepsTheScenarioRunning()
        {
            var session = RunningSession();

            session.ReachExit("south_exit", isCorrect: false);

            Assert.AreEqual(ScenarioState.Active, session.State);
            Assert.AreEqual(ScoreBoard.ValueOf(ScoreEvent.WrongExitSelected), session.Score);
            Assert.AreEqual("wrong_exit_selected", session.Events.Single().actionType);
        }

        [Test]
        public void RunningOutOfTimeFailsTheScenario()
        {
            var session = RunningSession(timeLimit: 10f);

            session.Tick(11f);

            Assert.AreEqual(ScenarioState.Failed, session.State);
        }

        [Test]
        public void ACriticalUnsafeActionFailsTheScenario()
        {
            var session = RunningSession();

            session.TriggerCriticalUnsafeAction("BlockedRoute_A");

            Assert.AreEqual(ScenarioState.Failed, session.State);
            Assert.AreEqual(ScoreBoard.ValueOf(ScoreEvent.CriticalUnsafeAction), session.Score);
        }

        [Test]
        public void ReviewAfterCompletionProducesTheResult()
        {
            var session = RunningSession();
            session.Tick(92.4f);
            session.ActivateAlarm("FireAlarm_A");
            session.ReachExit("north_exit", isCorrect: true);

            var result = session.EnterReview();

            Assert.AreEqual(ScenarioState.Review, session.State);
            Assert.AreEqual("fire_evacuation_01", result.scenarioId);
            Assert.IsTrue(result.completed);
            Assert.AreEqual(92.4f, result.completionTimeSeconds, 0.0001f);
            Assert.AreEqual("north_exit", result.selectedExit);
            Assert.AreEqual(session.Score, result.score);
            Assert.AreEqual(2, result.events.Count);
        }

        [Test]
        public void ReviewAfterFailureReportsAnIncompleteRun()
        {
            var session = RunningSession(timeLimit: 10f);
            session.Tick(11f);

            var result = session.EnterReview();

            Assert.IsFalse(result.completed);
            Assert.IsNull(result.selectedExit);
        }

        [Test]
        public void ResetReturnsToReadyAndClearsTheRun()
        {
            var session = RunningSession();
            session.Tick(20f);
            session.ActivateAlarm("FireAlarm_A");
            session.ReachExit("north_exit", isCorrect: true);
            session.EnterReview();

            session.ResetSession();

            Assert.AreEqual(ScenarioState.Ready, session.State);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0f, session.ElapsedSeconds);
            Assert.AreEqual(0, session.Events.Count);
        }

        [Test]
        public void InteractionsAreIgnoredBeforeTheScenarioStarts()
        {
            var session = NewSession();

            session.ActivateAlarm("FireAlarm_A");

            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.Events.Count);
        }

        [Test]
        public void InteractionsAreIgnoredAfterTheScenarioEnds()
        {
            var session = RunningSession();
            session.ReachExit("north_exit", isCorrect: true);

            session.ActivateAlarm("FireAlarm_A");

            Assert.AreEqual(1, session.Events.Count);
        }

        [Test]
        public void ReadingTheMapTicksOffThatObjective()
        {
            var session = RunningSession();

            session.ViewMap("EvacuationMap_A");

            Assert.IsTrue(session.Objectives.Objectives.Single(o => o.Id == "read_map").IsComplete);
        }

        [Test]
        public void RaisingTheAlarmTicksOffThatObjective()
        {
            var session = RunningSession();

            session.ActivateAlarm("FireAlarm_A");

            Assert.IsTrue(session.Objectives.Objectives.Single(o => o.Id == "raise_alarm").IsComplete);
        }

        [Test]
        public void ReachingTheSafeExitTicksOffThatObjective()
        {
            var session = RunningSession();

            session.ReachExit("north_exit", isCorrect: true);

            Assert.IsTrue(session.Objectives.Objectives.Single(o => o.Id == "reach_exit").IsComplete);
        }

        [Test]
        public void AvoidingTheSmokeCompletesOnlyIfTheUserNeverEnteredIt()
        {
            var clean = RunningSession();
            clean.ReachExit("north_exit", isCorrect: true);
            Assert.IsTrue(clean.Objectives.Objectives.Single(o => o.Id == "avoid_hazard").IsComplete);

            var smoky = RunningSession();
            smoky.EnterHazard("HazardZone_A");
            smoky.LeaveHazard("HazardZone_A");
            smoky.ReachExit("north_exit", isCorrect: true);
            Assert.IsFalse(smoky.Objectives.Objectives.Single(o => o.Id == "avoid_hazard").IsComplete);
        }

        [Test]
        public void ResettingClearsTheObjectives()
        {
            var session = RunningSession();
            session.ViewMap("EvacuationMap_A");

            session.ResetSession();

            Assert.AreEqual(0, session.Objectives.CompletedCount);
        }

        [Test]
        public void PausingStopsTheClock()
        {
            var session = RunningSession();
            session.Tick(5f);

            session.Pause();
            session.Tick(10f);

            Assert.IsTrue(session.IsPaused);
            Assert.AreEqual(5f, session.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void ResumingContinuesFromWhereItStopped()
        {
            var session = RunningSession();
            session.Tick(5f);
            session.Pause();

            session.Resume();
            session.Tick(2f);

            Assert.IsFalse(session.IsPaused);
            Assert.AreEqual(7f, session.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void InteractionsAreIgnoredWhilePaused()
        {
            var session = RunningSession();
            session.Pause();

            session.ActivateAlarm("FireAlarm_A");

            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.Events.Count);
        }

        [Test]
        public void PausingDoesNotChangeTheScenarioState()
        {
            var session = RunningSession();

            session.Pause();

            Assert.AreEqual(ScenarioState.Active, session.State,
                "pausing is not a scenario state; it suspends the run in place");
        }

        [Test]
        public void PausingBeforeTheRunStartsDoesNothing()
        {
            var session = NewSession();

            session.Pause();

            Assert.IsFalse(session.IsPaused);
        }

        [Test]
        public void AbandoningTheRunReturnsToReady()
        {
            var session = RunningSession();
            session.Tick(20f);
            session.ActivateAlarm("FireAlarm_A");

            session.Abandon();

            Assert.AreEqual(ScenarioState.Ready, session.State);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0f, session.ElapsedSeconds);
            Assert.IsFalse(session.IsPaused);
        }

        [Test]
        public void RaisesPausedChangedSoTheUiCanFollow()
        {
            var session = RunningSession();
            var observed = new System.Collections.Generic.List<bool>();
            session.PausedChanged += paused => observed.Add(paused);

            session.Pause();
            session.Resume();

            CollectionAssert.AreEqual(new[] { true, false }, observed);
        }

        [Test]
        public void RaisesStateChangedForTheUiToFollow()
        {
            var session = NewSession();
            var seen = new System.Collections.Generic.List<ScenarioState>();
            session.StateChanged += (previous, next) => seen.Add(next);

            session.BeginBriefing();
            session.StartScenario();

            CollectionAssert.AreEqual(new[] { ScenarioState.Briefing, ScenarioState.Active }, seen);
        }
    }
}
