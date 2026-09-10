using NUnit.Framework;
using VRSim.Evaluation;

namespace VRSim.Tests
{
    public class ScenarioTimerTests
    {
        [Test]
        public void StartsStoppedAtZero()
        {
            var timer = new ScenarioTimer();

            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.ElapsedSeconds);
        }

        [Test]
        public void DoesNotAccumulateBeforeItIsStarted()
        {
            var timer = new ScenarioTimer();

            timer.Tick(5f);

            Assert.AreEqual(0f, timer.ElapsedSeconds);
        }

        [Test]
        public void AccumulatesElapsedTimeWhileRunning()
        {
            var timer = new ScenarioTimer();

            timer.Start();
            timer.Tick(1.5f);
            timer.Tick(0.5f);

            Assert.IsTrue(timer.IsRunning);
            Assert.AreEqual(2f, timer.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void HoldsElapsedTimeWhilePaused()
        {
            var timer = new ScenarioTimer();
            timer.Start();
            timer.Tick(2f);

            timer.Pause();
            timer.Tick(10f);

            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(2f, timer.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void ContinuesFromTheHeldTimeAfterResuming()
        {
            var timer = new ScenarioTimer();
            timer.Start();
            timer.Tick(2f);
            timer.Pause();

            timer.Resume();
            timer.Tick(1f);

            Assert.AreEqual(3f, timer.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void ResetReturnsToZeroAndStops()
        {
            var timer = new ScenarioTimer();
            timer.Start();
            timer.Tick(4f);

            timer.Reset();

            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.ElapsedSeconds);
        }

        [Test]
        public void WithoutALimitItNeverExpires()
        {
            var timer = new ScenarioTimer();
            timer.Start();

            timer.Tick(10000f);

            Assert.IsFalse(timer.HasExpired);
        }

        [Test]
        public void ExpiresOnceTheLimitIsReached()
        {
            var timer = new ScenarioTimer(limitSeconds: 120f);
            timer.Start();

            timer.Tick(119f);
            Assert.IsFalse(timer.HasExpired);

            timer.Tick(1f);
            Assert.IsTrue(timer.HasExpired);
        }

        [Test]
        public void RaisesExpiredOnceWhenTheLimitIsPassed()
        {
            var timer = new ScenarioTimer(limitSeconds: 10f);
            var expiredCount = 0;
            timer.Expired += () => expiredCount++;
            timer.Start();

            timer.Tick(11f);
            timer.Tick(5f);

            Assert.AreEqual(1, expiredCount);
        }

        [Test]
        public void StopsRunningWhenTheLimitIsPassed()
        {
            var timer = new ScenarioTimer(limitSeconds: 10f);
            timer.Start();

            timer.Tick(11f);

            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void RemainingSecondsCountsDownToZero()
        {
            var timer = new ScenarioTimer(limitSeconds: 10f);
            timer.Start();

            timer.Tick(4f);
            Assert.AreEqual(6f, timer.RemainingSeconds, 0.0001f);

            timer.Tick(100f);
            Assert.AreEqual(0f, timer.RemainingSeconds, 0.0001f);
        }
    }
}
