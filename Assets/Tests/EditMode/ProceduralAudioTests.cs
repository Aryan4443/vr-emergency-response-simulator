using System.Linq;
using NUnit.Framework;
using VRSim.Core;

namespace VRSim.Tests
{
    public class ProceduralAudioTests
    {
        [Test]
        public void AToneHasTheRequestedLengthAndRate()
        {
            var clip = ProceduralAudio.Tone("test", frequency: 440f, seconds: 0.5f);

            Assert.AreEqual(0.5f, clip.length, 0.01f);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual(ProceduralAudio.SampleRate, clip.frequency);
        }

        [Test]
        public void AToneActuallyContainsSound()
        {
            var clip = ProceduralAudio.Tone("test", frequency: 440f, seconds: 0.2f);

            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            Assert.Greater(samples.Max(s => System.Math.Abs(s)), 0.1f, "the clip is silent");
        }

        [Test]
        public void EverySampleStaysWithinRange()
        {
            var clip = ProceduralAudio.Tone("test", frequency: 880f, seconds: 0.2f);

            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            Assert.IsTrue(samples.All(s => s >= -1f && s <= 1f), "a sample would clip");
        }

        [Test]
        public void AToneFadesInAndOutSoItDoesNotClick()
        {
            var clip = ProceduralAudio.Tone("test", frequency: 440f, seconds: 0.3f);

            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            Assert.Less(System.Math.Abs(samples[0]), 0.05f, "starts abruptly");
            Assert.Less(System.Math.Abs(samples[^1]), 0.05f, "ends abruptly");
        }

        [Test]
        public void TheAlarmIsATwoToneLoop()
        {
            var clip = ProceduralAudio.AlarmLoop();

            Assert.Greater(clip.length, 0.5f);
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);
            Assert.Greater(samples.Max(s => System.Math.Abs(s)), 0.1f);
        }

        [Test]
        public void EveryCueIsProducedAndNamed()
        {
            Assert.AreEqual("Alarm Loop", ProceduralAudio.AlarmLoop().name);
            Assert.AreEqual("Warning", ProceduralAudio.Warning().name);
            Assert.AreEqual("Interaction", ProceduralAudio.Interaction().name);
            Assert.AreEqual("Success", ProceduralAudio.Success().name);
            Assert.AreEqual("Failure", ProceduralAudio.Failure().name);
        }

        [Test]
        public void ASweepMovesBetweenTwoFrequencies()
        {
            var clip = ProceduralAudio.Sweep("rise", 220f, 660f, 0.4f);

            Assert.AreEqual(0.4f, clip.length, 0.01f);
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);
            Assert.Greater(samples.Max(s => System.Math.Abs(s)), 0.1f);
        }
    }
}
