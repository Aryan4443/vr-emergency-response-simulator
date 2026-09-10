using System.Collections.Generic;
using NUnit.Framework;
using VRSim.Scenario.Hazards;

namespace VRSim.Tests.Hazards
{
    public class AftershockScheduleTests
    {
        const float Tolerance = 0.0001f;

        /// <summary>
        /// A schedule whose ranges have no slack in them, so every start, duration and magnitude is
        /// known in advance: shocks run 5 to 7, 17 to 19 and 29 to 31 seconds. Used wherever the
        /// test is about the playback rules rather than about the generator.
        /// </summary>
        static AftershockSchedule CreateFixedSchedule(int seed = 1) => new AftershockSchedule(
            seed,
            shockCount: 3,
            firstShockDelaySeconds: 5f,
            minimumGapSeconds: 10f,
            maximumGapSeconds: 10f,
            minimumDurationSeconds: 2f,
            maximumDurationSeconds: 2f,
            minimumMagnitude: 0.5f,
            maximumMagnitude: 0.5f);

        [Test]
        public void KeepsTheSeedItWasBuiltWith()
        {
            var schedule = new AftershockSchedule(seed: 4242);

            Assert.AreEqual(4242, schedule.Seed);
        }

        [Test]
        public void GeneratesTheRequestedNumberOfShocks()
        {
            var schedule = new AftershockSchedule(seed: 3, shockCount: 6);

            Assert.AreEqual(6, schedule.Shocks.Count);
        }

        [Test]
        public void NumbersTheShocksInOrder()
        {
            var schedule = new AftershockSchedule(seed: 3, shockCount: 5);

            for (var i = 0; i < schedule.Shocks.Count; i++)
                Assert.AreEqual(i, schedule.Shocks[i].Index);
        }

        [Test]
        public void PlacesTheFirstShockAfterTheOpeningDelay()
        {
            var schedule = new AftershockSchedule(seed: 9, shockCount: 2, firstShockDelaySeconds: 12f);

            Assert.AreEqual(12f, schedule.Shocks[0].StartSeconds, Tolerance);
        }

        [Test]
        public void ShocksRunInOrderAndNeverOverlap()
        {
            var schedule = new AftershockSchedule(seed: 11, shockCount: 8);

            for (var i = 1; i < schedule.Shocks.Count; i++)
            {
                Assert.GreaterOrEqual(
                    schedule.Shocks[i].StartSeconds,
                    schedule.Shocks[i - 1].EndSeconds,
                    "Shock " + i + " starts before shock " + (i - 1) + " has finished.");
            }
        }

        [Test]
        public void ShockDurationsStayWithinTheRequestedRange()
        {
            var schedule = new AftershockSchedule(
                seed: 5,
                shockCount: 20,
                minimumDurationSeconds: 3f,
                maximumDurationSeconds: 8f);

            foreach (var shock in schedule.Shocks)
            {
                Assert.GreaterOrEqual(shock.DurationSeconds, 3f);
                Assert.LessOrEqual(shock.DurationSeconds, 8f);
            }
        }

        [Test]
        public void ShockMagnitudesStayWithinNoughtToOne()
        {
            var schedule = new AftershockSchedule(
                seed: 6,
                shockCount: 20,
                minimumMagnitude: -3f,
                maximumMagnitude: 9f);

            foreach (var shock in schedule.Shocks)
            {
                Assert.GreaterOrEqual(shock.Magnitude, 0f);
                Assert.LessOrEqual(shock.Magnitude, 1f);
            }
        }

        [Test]
        public void EndSecondsIsTheStartPlusTheDuration()
        {
            var shock = new Aftershock(index: 2, startSeconds: 10f, durationSeconds: 4f, magnitude: 0.5f);

            Assert.AreEqual(14f, shock.EndSeconds, Tolerance);
        }

        [Test]
        public void ReportsTheTotalDuration()
        {
            var schedule = CreateFixedSchedule();

            Assert.AreEqual(31f, schedule.TotalDurationSeconds, Tolerance);
        }

        [Test]
        public void StartsStillAndSilent()
        {
            var schedule = CreateFixedSchedule();

            Assert.IsFalse(schedule.IsShaking);
            Assert.AreEqual(0f, schedule.CurrentMagnitude, Tolerance);
            Assert.AreEqual(0f, schedule.ElapsedSeconds, Tolerance);
        }

        [Test]
        public void DoesNotShakeBeforeTheFirstShock()
        {
            var schedule = CreateFixedSchedule();
            var started = 0;
            schedule.ShakeStarted += _ => started++;

            schedule.Tick(4.9f);

            Assert.IsFalse(schedule.IsShaking);
            Assert.AreEqual(0, started);
        }

        [Test]
        public void ShakesWhenAShockBegins()
        {
            var schedule = CreateFixedSchedule();
            var started = new List<Aftershock>();
            schedule.ShakeStarted += shock => started.Add(shock);

            schedule.Tick(5f);

            Assert.IsTrue(schedule.IsShaking);
            Assert.AreEqual(0.5f, schedule.CurrentMagnitude, Tolerance);
            Assert.AreEqual(1, started.Count);
            Assert.AreEqual(0, started[0].Index);
        }

        [Test]
        public void StopsShakingWhenAShockEnds()
        {
            var schedule = CreateFixedSchedule();
            var ended = new List<Aftershock>();
            schedule.ShakeEnded += shock => ended.Add(shock);

            schedule.Tick(5f);
            schedule.Tick(2f);

            Assert.IsFalse(schedule.IsShaking);
            Assert.AreEqual(0f, schedule.CurrentMagnitude, Tolerance);
            Assert.AreEqual(1, ended.Count);
            Assert.AreEqual(0, ended[0].Index);
        }

        [Test]
        public void StaysStillBetweenShocks()
        {
            var schedule = CreateFixedSchedule();

            // Ten seconds in: the first shock finished at seven, the second starts at seventeen.
            schedule.Tick(10f);

            Assert.IsFalse(schedule.IsShaking);
            Assert.AreEqual(0f, schedule.CurrentMagnitude, Tolerance);
        }

        [Test]
        public void RaisesEachShockOnceAcrossManySmallTicks()
        {
            var schedule = CreateFixedSchedule();
            var started = 0;
            var ended = 0;
            schedule.ShakeStarted += _ => started++;
            schedule.ShakeEnded += _ => ended++;

            for (var i = 0; i < 400; i++)
                schedule.Tick(0.1f);

            Assert.AreEqual(3, started);
            Assert.AreEqual(3, ended);
            Assert.IsTrue(schedule.IsComplete);
        }

        [Test]
        public void ASingleLongTickDrainsTheWholeSchedule()
        {
            var schedule = CreateFixedSchedule();
            var started = 0;
            var ended = 0;
            schedule.ShakeStarted += _ => started++;
            schedule.ShakeEnded += _ => ended++;

            schedule.Tick(1000f);

            Assert.AreEqual(3, started);
            Assert.AreEqual(3, ended);
            Assert.IsFalse(schedule.IsShaking);
            Assert.IsTrue(schedule.IsComplete);
        }

        [Test]
        public void RaisesEventsInChronologicalOrderAcrossOneLongTick()
        {
            var schedule = CreateFixedSchedule();
            var log = new List<string>();
            schedule.ShakeStarted += shock => log.Add("start" + shock.Index);
            schedule.ShakeEnded += shock => log.Add("end" + shock.Index);

            schedule.Tick(1000f);

            CollectionAssert.AreEqual(
                new[] { "start0", "end0", "start1", "end1", "start2", "end2" },
                log);
        }

        [Test]
        public void IsNotCompleteUntilTheLastShockHasFinished()
        {
            var schedule = CreateFixedSchedule();

            schedule.Tick(30f);
            Assert.IsTrue(schedule.IsShaking);
            Assert.IsFalse(schedule.IsComplete);

            schedule.Tick(2f);
            Assert.IsTrue(schedule.IsComplete);
        }

        [Test]
        public void AnEmptyScheduleIsCompleteImmediately()
        {
            var schedule = new AftershockSchedule(seed: 1, shockCount: 0);

            Assert.AreEqual(0, schedule.Shocks.Count);
            Assert.AreEqual(0f, schedule.TotalDurationSeconds, Tolerance);
            Assert.IsTrue(schedule.IsComplete);

            Assert.DoesNotThrow(() => schedule.Tick(100f));
            Assert.IsFalse(schedule.IsShaking);
        }

        [Test]
        public void TicksSafelyWithNoListeners()
        {
            var schedule = CreateFixedSchedule();

            Assert.DoesNotThrow(() => schedule.Tick(1000f));
        }

        [Test]
        public void IgnoresNonPositiveTicks()
        {
            var schedule = CreateFixedSchedule();
            schedule.Tick(6f);

            schedule.Tick(0f);
            schedule.Tick(-20f);

            Assert.AreEqual(6f, schedule.ElapsedSeconds, Tolerance);
            Assert.IsTrue(schedule.IsShaking);
        }

        [Test]
        public void TheSameSeedGivesTheSameSchedule()
        {
            var first = new AftershockSchedule(seed: 2024, shockCount: 10);
            var second = new AftershockSchedule(seed: 2024, shockCount: 10);

            Assert.AreEqual(first.Shocks.Count, second.Shocks.Count);

            for (var i = 0; i < first.Shocks.Count; i++)
            {
                Assert.AreEqual(first.Shocks[i].StartSeconds, second.Shocks[i].StartSeconds, Tolerance);
                Assert.AreEqual(first.Shocks[i].DurationSeconds, second.Shocks[i].DurationSeconds, Tolerance);
                Assert.AreEqual(first.Shocks[i].Magnitude, second.Shocks[i].Magnitude, Tolerance);
            }
        }

        [Test]
        public void TheSameSeedGivesTheSamePlayback()
        {
            var first = new AftershockSchedule(seed: 77, shockCount: 5);
            var second = new AftershockSchedule(seed: 77, shockCount: 5);
            var firstLog = new List<string>();
            var secondLog = new List<string>();
            first.ShakeStarted += shock => firstLog.Add("start" + shock.Index);
            first.ShakeEnded += shock => firstLog.Add("end" + shock.Index);
            second.ShakeStarted += shock => secondLog.Add("start" + shock.Index);
            second.ShakeEnded += shock => secondLog.Add("end" + shock.Index);

            // Different tick sizes on purpose: the schedule is fixed by the seed, not by how the
            // frames happened to fall on the day.
            for (var i = 0; i < 4000; i++)
                first.Tick(0.1f);

            for (var i = 0; i < 400; i++)
                second.Tick(1f);

            Assert.AreEqual(10, firstLog.Count);
            CollectionAssert.AreEqual(firstLog, secondLog);
        }

        [Test]
        public void ADifferentSeedGivesADifferentSchedule()
        {
            var first = new AftershockSchedule(seed: 1, shockCount: 6);
            var second = new AftershockSchedule(seed: 2, shockCount: 6);

            var differs = false;

            for (var i = 0; i < first.Shocks.Count; i++)
            {
                if (Differs(first.Shocks[i].StartSeconds, second.Shocks[i].StartSeconds) ||
                    Differs(first.Shocks[i].DurationSeconds, second.Shocks[i].DurationSeconds) ||
                    Differs(first.Shocks[i].Magnitude, second.Shocks[i].Magnitude))
                {
                    differs = true;
                    break;
                }
            }

            Assert.IsTrue(differs, "Seeds 1 and 2 produced an identical schedule.");
        }

        [Test]
        public void NeighbouringSeedsGiveUnrelatedFirstShocks()
        {
            // Instructors reach for small, memorable seeds, so adjacent ones must not produce
            // near-identical drills.
            var first = new AftershockSchedule(seed: 1, shockCount: 1);
            var second = new AftershockSchedule(seed: 2, shockCount: 1);
            var third = new AftershockSchedule(seed: 3, shockCount: 1);

            Assert.IsTrue(Differs(first.Shocks[0].DurationSeconds, second.Shocks[0].DurationSeconds));
            Assert.IsTrue(Differs(second.Shocks[0].DurationSeconds, third.Shocks[0].DurationSeconds));
            Assert.IsTrue(Differs(first.Shocks[0].Magnitude, second.Shocks[0].Magnitude));
        }

        [Test]
        public void ResetReplaysTheSameSchedule()
        {
            var schedule = CreateFixedSchedule();
            var log = new List<string>();
            schedule.ShakeStarted += shock => log.Add("start" + shock.Index);
            schedule.ShakeEnded += shock => log.Add("end" + shock.Index);

            schedule.Tick(1000f);
            var firstRun = log.ToArray();

            log.Clear();
            schedule.Reset();
            schedule.Tick(1000f);

            CollectionAssert.AreEqual(firstRun, log);
        }

        [Test]
        public void ResetStopsAShakeInProgress()
        {
            var schedule = CreateFixedSchedule();
            schedule.Tick(6f);
            Assert.IsTrue(schedule.IsShaking);

            schedule.Reset();

            Assert.IsFalse(schedule.IsShaking);
            Assert.AreEqual(0f, schedule.CurrentMagnitude, Tolerance);
            Assert.AreEqual(0f, schedule.ElapsedSeconds, Tolerance);
            Assert.IsFalse(schedule.IsComplete);
        }

        [Test]
        public void ResetKeepsTheGeneratedShocks()
        {
            var schedule = new AftershockSchedule(seed: 31, shockCount: 4);
            var before = new List<float>();

            foreach (var shock in schedule.Shocks)
                before.Add(shock.StartSeconds);

            schedule.Tick(10000f);
            schedule.Reset();

            for (var i = 0; i < schedule.Shocks.Count; i++)
                Assert.AreEqual(before[i], schedule.Shocks[i].StartSeconds, Tolerance);
        }

        static bool Differs(float a, float b)
        {
            var difference = a - b;
            return (difference < 0f ? -difference : difference) > 0.001f;
        }
    }
}
