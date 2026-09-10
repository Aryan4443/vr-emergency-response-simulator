using System.Collections.Generic;
using NUnit.Framework;
using VRSim.Scenario.Hazards;

namespace VRSim.Tests.Hazards
{
    public class SpreadingHazardTests
    {
        const float Tolerance = 0.0001f;

        [Test]
        public void StartsWithNoReach()
        {
            var hazard = new SpreadingHazard(originPosition: 0f, spreadRate: 1f, maximumReach: 10f);

            Assert.AreEqual(0f, hazard.CurrentReach);
            Assert.AreEqual(0f, hazard.ElapsedSeconds);
        }

        [Test]
        public void KeepsTheValuesItWasBuiltWith()
        {
            var hazard = new SpreadingHazard(
                originPosition: 4f,
                spreadRate: 1.5f,
                maximumReach: 12f,
                startDelaySeconds: 3f);

            Assert.AreEqual(4f, hazard.OriginPosition, Tolerance);
            Assert.AreEqual(1.5f, hazard.SpreadRate, Tolerance);
            Assert.AreEqual(12f, hazard.MaximumReach, Tolerance);
            Assert.AreEqual(3f, hazard.StartDelaySeconds, Tolerance);
        }

        [Test]
        public void DoesNotSpreadWhileTheDelayIsRunning()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 2f, maximumReach: 50f, startDelaySeconds: 5f);

            hazard.Tick(2f);
            hazard.Tick(2f);

            Assert.IsFalse(hazard.HasStartedSpreading);
            Assert.AreEqual(0f, hazard.CurrentReach, Tolerance);
        }

        [Test]
        public void SpreadsOnlyForTheTimeLeftAfterTheDelay()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 2f, maximumReach: 50f, startDelaySeconds: 5f);

            // Eight seconds of ticks, of which only three are past the delay.
            hazard.Tick(8f);

            Assert.IsTrue(hazard.HasStartedSpreading);
            Assert.AreEqual(6f, hazard.CurrentReach, Tolerance);
        }

        [Test]
        public void SpreadsAtTheStatedRateOverManyTicks()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 2f, maximumReach: 100f);

            for (var i = 0; i < 10; i++)
                hazard.Tick(0.1f);

            Assert.AreEqual(2f, hazard.CurrentReach, 0.01f);
        }

        [Test]
        public void ClampsAtMaximumReach()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 3f, maximumReach: 6f);

            hazard.Tick(100f);

            Assert.AreEqual(6f, hazard.CurrentReach, Tolerance);
            Assert.IsTrue(hazard.IsAtMaximum);
        }

        [Test]
        public void IsNotAtMaximumWhileStillGrowing()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 10f);

            hazard.Tick(4f);

            Assert.IsFalse(hazard.IsAtMaximum);
        }

        [Test]
        public void TreatsANegativeSpreadRateAsStandingStill()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: -5f, maximumReach: 10f);

            hazard.Tick(10f);

            Assert.AreEqual(0f, hazard.SpreadRate, Tolerance);
            Assert.AreEqual(0f, hazard.CurrentReach, Tolerance);
        }

        [Test]
        public void TreatsANegativeDelayAsNoDelay()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 10f, startDelaySeconds: -4f);

            hazard.Tick(2f);

            Assert.AreEqual(0f, hazard.StartDelaySeconds, Tolerance);
            Assert.AreEqual(2f, hazard.CurrentReach, Tolerance);
        }

        [Test]
        public void ContainsTheOriginEvenBeforeItSpreads()
        {
            var hazard = new SpreadingHazard(originPosition: 7f, spreadRate: 1f, maximumReach: 10f);

            Assert.IsTrue(hazard.Contains(7f));
        }

        [Test]
        public void ContainsPositionsWithinTheCurrentReach()
        {
            var hazard = new SpreadingHazard(originPosition: 10f, spreadRate: 1f, maximumReach: 20f);

            hazard.Tick(4f);

            Assert.IsTrue(hazard.Contains(13f));
            Assert.IsFalse(hazard.Contains(16f));
        }

        [Test]
        public void SpreadsEquallyInBothDirections()
        {
            var hazard = new SpreadingHazard(originPosition: 10f, spreadRate: 1f, maximumReach: 20f);

            hazard.Tick(4f);

            Assert.IsTrue(hazard.Contains(6f));
            Assert.IsTrue(hazard.Contains(14f));
            Assert.IsFalse(hazard.Contains(5f));
            Assert.IsFalse(hazard.Contains(15f));
        }

        [Test]
        public void IgnoresNonPositiveTicks()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 10f);
            hazard.Tick(3f);

            hazard.Tick(0f);
            hazard.Tick(-5f);

            Assert.AreEqual(3f, hazard.ElapsedSeconds, Tolerance);
            Assert.AreEqual(3f, hazard.CurrentReach, Tolerance);
        }

        [Test]
        public void RaisesTheWatchPointOnceTheHazardCoversIt()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 20f);
            var reached = new List<float>();
            hazard.ReachedWatchPoint += position => reached.Add(position);
            hazard.AddWatchPoint(5f);

            hazard.Tick(4f);
            Assert.AreEqual(0, reached.Count);

            hazard.Tick(2f);
            Assert.AreEqual(1, reached.Count);
            Assert.AreEqual(5f, reached[0], Tolerance);
        }

        [Test]
        public void RaisesEachWatchPointExactlyOnce()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 20f);
            var reached = new List<float>();
            hazard.ReachedWatchPoint += position => reached.Add(position);
            hazard.AddWatchPoint(5f);

            hazard.Tick(6f);
            hazard.Tick(6f);
            hazard.Tick(6f);

            Assert.AreEqual(1, reached.Count);
        }

        [Test]
        public void RaisesWatchPointsNearestFirstWithinOneTick()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 10f, maximumReach: 100f);
            var reached = new List<float>();
            hazard.ReachedWatchPoint += position => reached.Add(position);

            // Registered furthest first on purpose: the order they fire in must come from the
            // corridor, not from the order a caller happened to register them.
            hazard.AddWatchPoint(9f);
            hazard.AddWatchPoint(3f);
            hazard.AddWatchPoint(-6f);

            hazard.Tick(1f);

            Assert.AreEqual(3, reached.Count);
            Assert.AreEqual(3f, reached[0], Tolerance);
            Assert.AreEqual(-6f, reached[1], Tolerance);
            Assert.AreEqual(9f, reached[2], Tolerance);
        }

        [Test]
        public void RaisesWatchPointsInOrderAcrossSeveralTicks()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 100f);
            var reached = new List<float>();
            hazard.ReachedWatchPoint += position => reached.Add(position);
            hazard.AddWatchPoint(2f);
            hazard.AddWatchPoint(4f);

            hazard.Tick(3f);
            Assert.AreEqual(1, reached.Count);

            hazard.Tick(3f);
            Assert.AreEqual(2, reached.Count);
            Assert.AreEqual(2f, reached[0], Tolerance);
            Assert.AreEqual(4f, reached[1], Tolerance);
        }

        [Test]
        public void NeverRaisesAWatchPointBeyondMaximumReach()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 5f, maximumReach: 4f);
            var reached = 0;
            hazard.ReachedWatchPoint += _ => reached++;
            hazard.AddWatchPoint(10f);

            hazard.Tick(1000f);

            Assert.AreEqual(0, reached);
        }

        [Test]
        public void RaisesAWatchPointOnTheOriginOnTheFirstTick()
        {
            var hazard = new SpreadingHazard(originPosition: 2f, spreadRate: 1f, maximumReach: 10f, startDelaySeconds: 30f);
            var reached = 0;
            hazard.ReachedWatchPoint += _ => reached++;
            hazard.AddWatchPoint(2f);

            hazard.Tick(0.1f);

            Assert.AreEqual(1, reached);
        }

        [Test]
        public void IgnoresADuplicateWatchPoint()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 20f);
            var reached = 0;
            hazard.ReachedWatchPoint += _ => reached++;
            hazard.AddWatchPoint(3f);
            hazard.AddWatchPoint(3f);

            hazard.Tick(10f);

            Assert.AreEqual(1, reached);
        }

        [Test]
        public void TicksSafelyWithNoWatchPointListener()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 20f);
            hazard.AddWatchPoint(3f);

            Assert.DoesNotThrow(() => hazard.Tick(10f));
        }

        [Test]
        public void ResetReturnsTheHazardToItsOrigin()
        {
            var hazard = new SpreadingHazard(originPosition: 1f, spreadRate: 2f, maximumReach: 20f);
            hazard.Tick(5f);

            hazard.Reset();

            Assert.AreEqual(0f, hazard.ElapsedSeconds);
            Assert.AreEqual(0f, hazard.CurrentReach);
            Assert.IsFalse(hazard.Contains(5f));
        }

        [Test]
        public void ResetReArmsEveryWatchPoint()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 20f);
            var reached = 0;
            hazard.ReachedWatchPoint += _ => reached++;
            hazard.AddWatchPoint(3f);
            hazard.Tick(10f);
            Assert.AreEqual(1, reached);

            hazard.Reset();
            hazard.Tick(10f);

            Assert.AreEqual(2, reached);
        }

        [Test]
        public void ResetRestoresTheDelayAsWell()
        {
            var hazard = new SpreadingHazard(0f, spreadRate: 1f, maximumReach: 20f, startDelaySeconds: 5f);
            hazard.Tick(10f);

            hazard.Reset();
            hazard.Tick(2f);

            Assert.IsFalse(hazard.HasStartedSpreading);
            Assert.AreEqual(0f, hazard.CurrentReach, Tolerance);
        }
    }
}
