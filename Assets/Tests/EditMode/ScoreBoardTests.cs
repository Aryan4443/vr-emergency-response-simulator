using NUnit.Framework;
using VRSim.Evaluation;

namespace VRSim.Tests
{
    public class ScoreBoardTests
    {
        [Test]
        public void StartsAtZero()
        {
            var board = new ScoreBoard();

            Assert.AreEqual(0, board.Total);
        }

        [Test]
        public void AwardsTheSpecifiedValueForEachScoringEvent()
        {
            Assert.AreEqual(40, ScoreBoard.ValueOf(ScoreEvent.CorrectSafeExit));
            Assert.AreEqual(20, ScoreBoard.ValueOf(ScoreEvent.HazardIdentified));
            Assert.AreEqual(10, ScoreBoard.ValueOf(ScoreEvent.AlarmActivated));
            Assert.AreEqual(5, ScoreBoard.ValueOf(ScoreEvent.EvacuationMapViewed));
            Assert.AreEqual(-15, ScoreBoard.ValueOf(ScoreEvent.UnsafeAreaEntered));
            Assert.AreEqual(-25, ScoreBoard.ValueOf(ScoreEvent.WrongExitSelected));
            Assert.AreEqual(-30, ScoreBoard.ValueOf(ScoreEvent.CriticalUnsafeAction));
            Assert.AreEqual(30, ScoreBoard.ValueOf(ScoreEvent.SuccessfulCompletion));
        }

        [Test]
        public void AddsAnAwardToTheTotal()
        {
            var board = new ScoreBoard();

            board.Record(ScoreEvent.AlarmActivated);

            Assert.AreEqual(10, board.Total);
        }

        [Test]
        public void AwardsEachPositiveEventOnlyOnce()
        {
            var board = new ScoreBoard();

            board.Record(ScoreEvent.AlarmActivated);
            board.Record(ScoreEvent.AlarmActivated);
            board.Record(ScoreEvent.AlarmActivated);

            Assert.AreEqual(10, board.Total);
        }

        [Test]
        public void PenalisesEveryUnsafeEntry()
        {
            var board = new ScoreBoard();

            board.Record(ScoreEvent.UnsafeAreaEntered);
            board.Record(ScoreEvent.UnsafeAreaEntered);

            Assert.AreEqual(-30, board.Total);
        }

        [Test]
        public void CountsHowManyTimesAnEventWasRecorded()
        {
            var board = new ScoreBoard();

            board.Record(ScoreEvent.UnsafeAreaEntered);
            board.Record(ScoreEvent.UnsafeAreaEntered);
            board.Record(ScoreEvent.WrongExitSelected);

            Assert.AreEqual(2, board.CountOf(ScoreEvent.UnsafeAreaEntered));
            Assert.AreEqual(1, board.CountOf(ScoreEvent.WrongExitSelected));
            Assert.AreEqual(0, board.CountOf(ScoreEvent.AlarmActivated));
        }

        [Test]
        public void ReportsANegativeTotalWhenPenaltiesOutweighRewards()
        {
            var board = new ScoreBoard();

            board.Record(ScoreEvent.AlarmActivated);
            board.Record(ScoreEvent.CriticalUnsafeAction);
            board.Record(ScoreEvent.CriticalUnsafeAction);

            Assert.AreEqual(-50, board.Total);
        }

        [Test]
        public void RaisesScoreChangedWithTheNewTotal()
        {
            var board = new ScoreBoard();
            var observed = -1;
            board.ScoreChanged += total => observed = total;

            board.Record(ScoreEvent.EvacuationMapViewed);

            Assert.AreEqual(5, observed);
        }

        [Test]
        public void ResetClearsTheTotalAndTheCounts()
        {
            var board = new ScoreBoard();
            board.Record(ScoreEvent.AlarmActivated);

            board.Reset();

            Assert.AreEqual(0, board.Total);
            Assert.AreEqual(0, board.CountOf(ScoreEvent.AlarmActivated));
        }

        [Test]
        public void AwardsAnEventAgainAfterAReset()
        {
            var board = new ScoreBoard();
            board.Record(ScoreEvent.AlarmActivated);
            board.Reset();

            board.Record(ScoreEvent.AlarmActivated);

            Assert.AreEqual(10, board.Total);
        }
    }
}
