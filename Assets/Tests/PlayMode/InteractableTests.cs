using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRSim.Interaction;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class InteractableTests
    {
        GameObject m_ManagerObject;
        ScenarioManager m_Manager;

        [SetUp]
        public void SetUp()
        {
            m_ManagerObject = new GameObject("ScenarioManager");
            m_Manager = m_ManagerObject.AddComponent<ScenarioManager>();
            m_Manager.BeginBriefing();
            m_Manager.StartScenario();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(m_ManagerObject);

        T CreateInteractable<T>(string id) where T : InteractableObject
        {
            var go = new GameObject(id);
            var component = go.AddComponent<T>();
            component.ObjectId = id;
            component.ScenarioManager = m_Manager;
            return component;
        }

        [UnityTest]
        public IEnumerator ActivatingTheFireAlarmScoresLogsAndLatches()
        {
            var alarm = CreateInteractable<FireAlarm>("FireAlarm_A");
            yield return null;

            alarm.Interact();

            Assert.IsTrue(alarm.IsActivated);
            Assert.AreEqual(10, m_Manager.Score);
            Assert.AreEqual("alarm_activated", m_Manager.Session.Events.Single().actionType);

            Object.Destroy(alarm.gameObject);
        }

        [UnityTest]
        public IEnumerator TheFireAlarmOnlyRespondsToTheFirstActivation()
        {
            var alarm = CreateInteractable<FireAlarm>("FireAlarm_A");
            yield return null;

            alarm.Interact();
            alarm.Interact();

            Assert.AreEqual(1, m_Manager.Session.Events.Count);

            Object.Destroy(alarm.gameObject);
        }

        [UnityTest]
        public IEnumerator InspectingTheEvacuationMapScoresAndLogs()
        {
            var map = CreateInteractable<EvacuationMap>("EvacuationMap_A");
            yield return null;

            map.Interact();

            Assert.AreEqual(5, m_Manager.Score);
            Assert.AreEqual("map_viewed", m_Manager.Session.Events.Single().actionType);

            Object.Destroy(map.gameObject);
        }

        [UnityTest]
        public IEnumerator OpeningADoorLogsItAndFlipsTheOpenState()
        {
            var door = CreateInteractable<InteractiveDoor>("Door_Hallway");
            yield return null;

            door.Interact();

            Assert.IsTrue(door.IsOpen);
            Assert.AreEqual("door_opened", m_Manager.Session.Events.Single().actionType);

            Object.Destroy(door.gameObject);
        }

        [UnityTest]
        public IEnumerator ADoorCanBeClosedAgainWithoutLoggingASecondOpening()
        {
            var door = CreateInteractable<InteractiveDoor>("Door_Hallway");
            yield return null;

            door.Interact();
            door.Interact();

            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(1, m_Manager.Session.Events.Count(e => e.actionType == "door_opened"));

            Object.Destroy(door.gameObject);
        }

        [UnityTest]
        public IEnumerator ABlockedRouteReportsACriticalUnsafeAction()
        {
            var blocked = CreateInteractable<BlockedRoute>("BlockedRoute_A");
            yield return null;

            blocked.Interact();

            Assert.AreEqual(ScenarioState.Failed, m_Manager.State);

            Object.Destroy(blocked.gameObject);
        }
    }
}
