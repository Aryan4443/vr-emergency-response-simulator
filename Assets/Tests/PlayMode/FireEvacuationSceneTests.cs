using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRSim.Evaluation;
using VRSim.Interaction;
using VRSim.Scenario;

namespace VRSim.Tests
{
    /// <summary>
    /// Regression cover for section 15: the built scenario still runs from beginning to end.
    /// Walks the player body through the graybox rather than exercising the classes directly.
    /// </summary>
    public class FireEvacuationSceneTests
    {
        ScenarioManager m_Manager;
        Transform m_PlayerBody;
        ScenarioResult m_Result;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("FireEvacuation", LoadSceneMode.Single);
            yield return null;

            m_Manager = Object.FindAnyObjectByType<ScenarioManager>();
            Assert.IsNotNull(m_Manager, "The scene has no Scenario Manager.");

            var body = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
            Assert.IsNotNull(body, "The scene has no object tagged Player.");
            m_PlayerBody = body.transform;

            // The follower keeps the collider under the headset, which would drag it straight back
            // to the rig. Tests move the body directly instead.
            var follower = body.GetComponent<PlayerBodyFollower>();
            if (follower != null)
                follower.enabled = false;

            // The HUD opens the results screen itself when a run ends, so the test listens for
            // the summary instead of asking for it.
            m_Result = null;
            m_Manager.ResultReady += result => m_Result = result;

            m_Manager.BeginBriefing();
            m_Manager.StartScenario();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Leaving the scenario scene loaded would let its player body and hazard volumes take
            // part in later fixtures, so it is swapped for an empty scene after every test.
            var empty = SceneManager.CreateScene("EmptyAfterSceneTest");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("FireEvacuation");
        }

        IEnumerator MoveTo(Vector3 position)
        {
            m_PlayerBody.position = position;
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [Test]
        public void TheSceneContainsEveryMvpObject()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<FireAlarm>(), "no fire alarm");
            Assert.IsNotNull(Object.FindAnyObjectByType<EvacuationMap>(), "no evacuation map");
            Assert.IsNotNull(Object.FindAnyObjectByType<InteractiveDoor>(), "no interactive door");
            Assert.IsNotNull(Object.FindAnyObjectByType<HazardZone>(), "no hazard zone");
            Assert.IsNotNull(Object.FindAnyObjectByType<BlockedRoute>(), "no blocked route");

            var exits = Object.FindObjectsByType<ExitZone>(FindObjectsSortMode.None);
            Assert.AreEqual(2, exits.Length, "expected one safe exit and one wrong exit");
            Assert.AreEqual(1, exits.Count(e => e.IsCorrectExit), "expected exactly one correct exit");
        }

        [UnityTest]
        public IEnumerator TheUserCanRunTheScenarioFromBriefingToASafeExit()
        {
            Object.FindAnyObjectByType<EvacuationMap>().Interact();
            Object.FindAnyObjectByType<FireAlarm>().Interact();

            yield return MoveTo(new Vector3(0f, 0f, 13f));
            yield return null;

            Assert.AreEqual(ScenarioState.Review, m_Manager.State, "the HUD opens the results screen");
            Assert.IsNotNull(m_Result, "no result was published");
            Assert.IsTrue(m_Result.completed);
            Assert.AreEqual("north_exit", m_Result.selectedExit);
            Assert.AreEqual(0, m_Result.unsafeZoneEntries);
            Assert.AreEqual(85, m_Result.score, "5 map + 10 alarm + 40 exit + 30 completion");
        }

        [UnityTest]
        public IEnumerator WalkingIntoTheSmokeWarnsAndPenalisesTheRun()
        {
            yield return MoveTo(new Vector3(0f, 0f, -8.5f));

            Assert.AreEqual(ScenarioState.Warning, m_Manager.State);
            Assert.AreEqual(-15, m_Manager.Score);

            yield return MoveTo(new Vector3(0f, 0f, 0f));

            Assert.AreEqual(ScenarioState.Active, m_Manager.State);
        }

        [UnityTest]
        public IEnumerator TakingTheSouthExitIsPenalisedButRecoverable()
        {
            yield return MoveTo(new Vector3(0f, 0f, -13f));

            Assert.AreEqual(-25, m_Manager.Score);

            yield return MoveTo(new Vector3(0f, 0f, 13f));
            yield return null;

            Assert.IsNotNull(m_Result);
            Assert.IsTrue(m_Result.completed);
        }

        [UnityTest]
        public IEnumerator ResettingReturnsTheScenarioToReady()
        {
            yield return MoveTo(new Vector3(0f, 0f, 13f));
            yield return null;

            m_Manager.ResetScenario();

            Assert.AreEqual(ScenarioState.Ready, m_Manager.State);
            Assert.AreEqual(0, m_Manager.Score);
            Assert.AreEqual(0, m_Manager.Session.Events.Count);
        }
    }
}
