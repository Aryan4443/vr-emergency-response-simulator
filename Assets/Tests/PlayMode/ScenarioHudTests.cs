using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRSim.Scenario;
using VRSim.UI;

namespace VRSim.Tests
{
    /// <summary>
    /// Covers the briefing to results flow as the user drives it, without the test reaching past
    /// the HUD to start the scenario itself.
    /// </summary>
    public class ScenarioHudTests
    {
        ScenarioManager m_Manager;
        ScenarioHud m_Hud;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("FireEvacuation", LoadSceneMode.Single);
            yield return null;

            m_Manager = Object.FindAnyObjectByType<ScenarioManager>();
            m_Hud = Object.FindAnyObjectByType<ScenarioHud>();
            Assert.IsNotNull(m_Hud, "the scene has no HUD");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var empty = SceneManager.CreateScene("EmptyAfterHudTest");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("FireEvacuation");
        }

        [Test]
        public void TheScenarioIsBriefingWhenTheSceneOpens()
        {
            Assert.AreEqual(ScenarioState.Briefing, m_Manager.State);
        }

        [UnityTest]
        public IEnumerator TheStartButtonRunsTheScenario()
        {
            m_Hud.StartScenario();
            yield return null;

            Assert.AreEqual(ScenarioState.Active, m_Manager.State);
        }

        [UnityTest]
        public IEnumerator TheClockRunsOnceTheDrillHasStarted()
        {
            m_Hud.StartScenario();

            yield return new WaitForSeconds(0.25f);

            Assert.Greater(m_Manager.ElapsedSeconds, 0f);
        }

        [UnityTest]
        public IEnumerator ReplayReturnsToABriefingThatCanBeStartedAgain()
        {
            m_Hud.StartScenario();
            yield return null;

            m_Hud.Replay();
            yield return null;
            Assert.AreEqual(ScenarioState.Briefing, m_Manager.State);

            m_Hud.StartScenario();
            yield return null;
            Assert.AreEqual(ScenarioState.Active, m_Manager.State);
        }
    }
}
