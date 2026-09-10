using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRSim.Core;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class AudioManagerTests
    {
        GameObject m_ManagerObject;
        GameObject m_AudioObject;
        ScenarioManager m_Manager;
        AudioManager m_Audio;

        [SetUp]
        public void SetUp()
        {
            m_ManagerObject = new GameObject("ScenarioManager");
            m_Manager = m_ManagerObject.AddComponent<ScenarioManager>();

            m_AudioObject = new GameObject("AudioManager");
            m_Audio = m_AudioObject.AddComponent<AudioManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_AudioObject);
            Object.Destroy(m_ManagerObject);
        }

        [UnityTest]
        public IEnumerator ItBuildsItsOwnSourcesAndClipsWhenTheSceneSuppliesNone()
        {
            yield return null;

            var sources = m_AudioObject.GetComponents<AudioSource>();
            Assert.GreaterOrEqual(sources.Length, 2, "expected an alarm source and an effects source");
        }

        [UnityTest]
        public IEnumerator TheAlarmPlaysWhenItIsStarted()
        {
            yield return null;

            m_Audio.StartAlarm();

            var playing = false;
            foreach (var source in m_AudioObject.GetComponents<AudioSource>())
                playing |= source.isPlaying;

            Assert.IsTrue(playing, "no audio source started");
        }

        [UnityTest]
        public IEnumerator StartingTheAlarmRaisesASubtitle()
        {
            string subtitle = null;
            m_Audio.SubtitleRequested += line => subtitle = line;
            yield return null;

            m_Audio.StartAlarm();

            Assert.IsNotNull(subtitle);
            StringAssert.Contains("alarm", subtitle.ToLowerInvariant());
        }

        [UnityTest]
        public IEnumerator StoppingTheAlarmSilencesIt()
        {
            yield return null;
            m_Audio.StartAlarm();

            m_Audio.StopAlarm();

            foreach (var source in m_AudioObject.GetComponents<AudioSource>())
                Assert.IsFalse(source.isPlaying && source.loop, "the alarm is still looping");
        }
    }
}
