using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRSim.Interaction;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class ExitZoneTests
    {
        GameObject m_ManagerObject;
        GameObject m_PlayerObject;
        GameObject m_ExitObject;

        [SetUp]
        public void SetUp()
        {
            m_ManagerObject = new GameObject("ScenarioManager");
            var manager = m_ManagerObject.AddComponent<ScenarioManager>();
            manager.BeginBriefing();
            manager.StartScenario();

            m_PlayerObject = new GameObject("Player") { tag = "Player" };
            m_PlayerObject.AddComponent<BoxCollider>();
            m_PlayerObject.AddComponent<Rigidbody>().useGravity = false;
            m_PlayerObject.transform.position = new Vector3(0f, 0f, 20f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_ExitObject);
            Object.Destroy(m_PlayerObject);
            Object.Destroy(m_ManagerObject);
        }

        ExitZone CreateExit(string exitId, bool isCorrect)
        {
            m_ExitObject = new GameObject(exitId);
            var collider = m_ExitObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 4f, 4f);
            var exit = m_ExitObject.AddComponent<ExitZone>();
            exit.ExitId = exitId;
            exit.IsCorrectExit = isCorrect;
            exit.ScenarioManager = m_ManagerObject.GetComponent<ScenarioManager>();
            m_ExitObject.transform.position = Vector3.zero;
            return exit;
        }

        [UnityTest]
        public IEnumerator ReachingTheCorrectExitCompletesTheScenario()
        {
            var session = m_ManagerObject.GetComponent<ScenarioManager>().Session;
            CreateExit("north_exit", isCorrect: true);

            m_PlayerObject.transform.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(ScenarioState.Completed, session.State);
        }

        [UnityTest]
        public IEnumerator ReachingTheWrongExitPenalisesButKeepsTheRunGoing()
        {
            var session = m_ManagerObject.GetComponent<ScenarioManager>().Session;
            CreateExit("south_exit", isCorrect: false);

            m_PlayerObject.transform.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(ScenarioState.Active, session.State);
            Assert.AreEqual(-25, session.Score);
        }

        [UnityTest]
        public IEnumerator TheWrongExitIsOnlyPenalisedOncePerVisit()
        {
            var session = m_ManagerObject.GetComponent<ScenarioManager>().Session;
            CreateExit("south_exit", isCorrect: false);

            m_PlayerObject.transform.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(-25, session.Score);
        }
    }
}
