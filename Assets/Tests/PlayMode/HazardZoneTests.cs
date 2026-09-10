using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRSim.Interaction;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class HazardZoneTests
    {
        GameObject m_ManagerObject;
        GameObject m_ZoneObject;
        GameObject m_PlayerObject;

        [SetUp]
        public void SetUp()
        {
            m_ManagerObject = new GameObject("ScenarioManager");
            var manager = m_ManagerObject.AddComponent<ScenarioManager>();
            manager.BeginBriefing();
            manager.StartScenario();

            m_ZoneObject = new GameObject("HazardZone_A");
            var collider = m_ZoneObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 4f, 4f);
            var zone = m_ZoneObject.AddComponent<HazardZone>();
            zone.ZoneId = "HazardZone_A";
            zone.ScenarioManager = manager;
            m_ZoneObject.transform.position = Vector3.zero;

            m_PlayerObject = new GameObject("Player");
            m_PlayerObject.tag = "Player";
            m_PlayerObject.AddComponent<BoxCollider>();
            var body = m_PlayerObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            m_PlayerObject.transform.position = new Vector3(0f, 0f, 20f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_PlayerObject);
            Object.Destroy(m_ZoneObject);
            Object.Destroy(m_ManagerObject);
        }

        [UnityTest]
        public IEnumerator EnteringTheZonePenalisesTheRunAndRaisesAWarning()
        {
            var session = m_ManagerObject.GetComponent<ScenarioManager>().Session;

            m_PlayerObject.transform.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(ScenarioState.Warning, session.State);
            Assert.AreEqual(-15, session.Score);
        }

        [UnityTest]
        public IEnumerator LeavingTheZoneClearsTheWarning()
        {
            var session = m_ManagerObject.GetComponent<ScenarioManager>().Session;

            m_PlayerObject.transform.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return null;

            m_PlayerObject.transform.position = new Vector3(0f, 0f, 20f);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(ScenarioState.Active, session.State);
        }

        [UnityTest]
        public IEnumerator IgnoresObjectsThatAreNotThePlayer()
        {
            var session = m_ManagerObject.GetComponent<ScenarioManager>().Session;
            var prop = new GameObject("Prop");
            prop.AddComponent<BoxCollider>();
            prop.AddComponent<Rigidbody>().useGravity = false;

            prop.transform.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(ScenarioState.Active, session.State);
            Assert.AreEqual(0, session.Score);

            Object.Destroy(prop);
        }
    }
}
