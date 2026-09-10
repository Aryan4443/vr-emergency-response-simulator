using NUnit.Framework;
using VRSim.Scenario.Hazards;

namespace VRSim.Tests.Hazards
{
    public class HazardIntensityTests
    {
        const float Tolerance = 0.0001f;

        [Test]
        public void StartsAtNothing()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 5f);

            Assert.AreEqual(0f, intensity.CurrentIntensity, Tolerance);
            Assert.AreEqual(0f, intensity.Progress, Tolerance);
            Assert.IsFalse(intensity.IsAtPeak);
        }

        [Test]
        public void KeepsTheValuesItWasBuiltWith()
        {
            var intensity = new HazardIntensity(
                riseSeconds: 8f,
                falloffDistance: 6f,
                curve: HazardIntensityCurve.EaseIn,
                peakIntensity: 0.8f,
                survivableThreshold: 0.4f);

            Assert.AreEqual(8f, intensity.RiseSeconds, Tolerance);
            Assert.AreEqual(6f, intensity.FalloffDistance, Tolerance);
            Assert.AreEqual(HazardIntensityCurve.EaseIn, intensity.Curve);
            Assert.AreEqual(0.8f, intensity.PeakIntensity, Tolerance);
            Assert.AreEqual(0.4f, intensity.SurvivableThreshold, Tolerance);
        }

        [Test]
        public void RisesLinearlyTowardsThePeak()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 0f);

            intensity.Tick(2.5f);
            Assert.AreEqual(0.25f, intensity.CurrentIntensity, Tolerance);

            intensity.Tick(2.5f);
            Assert.AreEqual(0.5f, intensity.CurrentIntensity, Tolerance);

            intensity.Tick(5f);
            Assert.AreEqual(1f, intensity.CurrentIntensity, Tolerance);
        }

        [Test]
        public void ClampsAtThePeakOnceTheRiseIsOver()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 0f);

            intensity.Tick(1000f);

            Assert.AreEqual(1f, intensity.CurrentIntensity, Tolerance);
            Assert.IsTrue(intensity.IsAtPeak);
        }

        [Test]
        public void EaseInLagsBehindLinearPartWayThroughTheRise()
        {
            var linear = new HazardIntensity(10f, 0f, HazardIntensityCurve.Linear);
            var easeIn = new HazardIntensity(10f, 0f, HazardIntensityCurve.EaseIn);

            linear.Tick(5f);
            easeIn.Tick(5f);

            Assert.AreEqual(0.5f, linear.CurrentIntensity, Tolerance);
            Assert.AreEqual(0.25f, easeIn.CurrentIntensity, Tolerance);
            Assert.Less(easeIn.CurrentIntensity, linear.CurrentIntensity);
        }

        [Test]
        public void EaseInAcceleratesTowardsTheEndOfTheRise()
        {
            var easeIn = new HazardIntensity(10f, 0f, HazardIntensityCurve.EaseIn);

            easeIn.Tick(2f);
            var firstFifth = easeIn.CurrentIntensity;

            easeIn.Tick(6f);
            var beforeLastFifth = easeIn.CurrentIntensity;

            easeIn.Tick(2f);
            var lastFifth = easeIn.CurrentIntensity - beforeLastFifth;

            Assert.AreEqual(0.04f, firstFifth, Tolerance);
            Assert.Greater(lastFifth, firstFifth);
        }

        [Test]
        public void BothCurvesEndAtTheSamePeak()
        {
            var linear = new HazardIntensity(10f, 0f, HazardIntensityCurve.Linear);
            var easeIn = new HazardIntensity(10f, 0f, HazardIntensityCurve.EaseIn);

            linear.Tick(10f);
            easeIn.Tick(10f);

            Assert.AreEqual(1f, linear.CurrentIntensity, Tolerance);
            Assert.AreEqual(1f, easeIn.CurrentIntensity, Tolerance);
        }

        [Test]
        public void NeverClimbsAboveTheStatedPeak()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 0f, peakIntensity: 0.6f);

            intensity.Tick(1000f);

            Assert.AreEqual(0.6f, intensity.CurrentIntensity, Tolerance);
        }

        [Test]
        public void ClampsAnOutOfRangePeakToNoughtToOne()
        {
            var tooHigh = new HazardIntensity(1f, 0f, peakIntensity: 5f);
            var tooLow = new HazardIntensity(1f, 0f, peakIntensity: -2f);

            Assert.AreEqual(1f, tooHigh.PeakIntensity, Tolerance);
            Assert.AreEqual(0f, tooLow.PeakIntensity, Tolerance);
        }

        [Test]
        public void IsAtThePeakImmediatelyWhenThereIsNoRise()
        {
            var intensity = new HazardIntensity(riseSeconds: 0f, falloffDistance: 0f);

            Assert.IsTrue(intensity.IsAtPeak);
            Assert.AreEqual(1f, intensity.CurrentIntensity, Tolerance);
        }

        [Test]
        public void FallsOffLinearlyWithDistance()
        {
            var intensity = new HazardIntensity(riseSeconds: 0f, falloffDistance: 10f);

            Assert.AreEqual(1f, intensity.IntensityAt(0f), Tolerance);
            Assert.AreEqual(0.75f, intensity.IntensityAt(2.5f), Tolerance);
            Assert.AreEqual(0.5f, intensity.IntensityAt(5f), Tolerance);
            Assert.AreEqual(0.25f, intensity.IntensityAt(7.5f), Tolerance);
        }

        [Test]
        public void IsNothingAtAndBeyondTheFalloffDistance()
        {
            var intensity = new HazardIntensity(riseSeconds: 0f, falloffDistance: 10f);

            Assert.AreEqual(0f, intensity.IntensityAt(10f), Tolerance);
            Assert.AreEqual(0f, intensity.IntensityAt(40f), Tolerance);
        }

        [Test]
        public void IgnoresTheSignOfTheDistance()
        {
            var intensity = new HazardIntensity(riseSeconds: 0f, falloffDistance: 10f);

            Assert.AreEqual(intensity.IntensityAt(4f), intensity.IntensityAt(-4f), Tolerance);
        }

        [Test]
        public void IntensityAtTheOriginMatchesCurrentIntensity()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 10f);

            intensity.Tick(3f);

            Assert.AreEqual(intensity.CurrentIntensity, intensity.IntensityAt(0f), Tolerance);
        }

        [Test]
        public void IsUniformWhenNoFalloffDistanceIsGiven()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 0f);

            intensity.Tick(5f);

            Assert.AreEqual(0.5f, intensity.IntensityAt(0f), Tolerance);
            Assert.AreEqual(0.5f, intensity.IntensityAt(1000f), Tolerance);
        }

        [Test]
        public void CombinesTheRiseAndTheFalloff()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 10f);

            intensity.Tick(5f);

            // Half way through the rise, half way to the falloff distance: a quarter of the peak.
            Assert.AreEqual(0.25f, intensity.IntensityAt(5f), Tolerance);
        }

        [Test]
        public void StaysSurvivableUntilTheThresholdIsCrossed()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 0f, survivableThreshold: 0.75f);

            intensity.Tick(7f);
            Assert.IsTrue(intensity.IsSurvivableAt(0f));

            intensity.Tick(1f);
            Assert.IsFalse(intensity.IsSurvivableAt(0f));
        }

        [Test]
        public void StaysSurvivableFurtherFromTheOrigin()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 10f, survivableThreshold: 0.75f);

            intensity.Tick(10f);

            Assert.IsFalse(intensity.IsSurvivableAt(0f));
            Assert.IsTrue(intensity.IsSurvivableAt(5f));
        }

        [Test]
        public void IgnoresNonPositiveTicks()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 0f);
            intensity.Tick(4f);

            intensity.Tick(0f);
            intensity.Tick(-10f);

            Assert.AreEqual(4f, intensity.ElapsedSeconds, Tolerance);
            Assert.AreEqual(0.4f, intensity.CurrentIntensity, Tolerance);
        }

        [Test]
        public void ResetReturnsToNothing()
        {
            var intensity = new HazardIntensity(riseSeconds: 10f, falloffDistance: 10f);
            intensity.Tick(9f);

            intensity.Reset();

            Assert.AreEqual(0f, intensity.ElapsedSeconds, Tolerance);
            Assert.AreEqual(0f, intensity.CurrentIntensity, Tolerance);
            Assert.IsTrue(intensity.IsSurvivableAt(0f));
        }
    }
}
