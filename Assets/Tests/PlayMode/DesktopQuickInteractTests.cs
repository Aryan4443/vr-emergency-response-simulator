using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRSim.Core;
using VRSim.Interaction;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class DesktopQuickInteractTests
    {
        GameObject m_Root;
        GameObject m_ManagerObject;
        ScenarioManager m_Manager;
        DesktopQuickInteract m_Quick;
        Transform m_RightHand;
        Transform m_LeftHand;

        [SetUp]
        public void SetUp()
        {
            m_ManagerObject = new GameObject("ScenarioManager");
            m_Manager = m_ManagerObject.AddComponent<ScenarioManager>();
            m_Manager.BeginBriefing();
            m_Manager.StartScenario();

            m_Root = new GameObject("QuickInteract");
            m_RightHand = new GameObject("Right Hand").transform;
            m_LeftHand = new GameObject("Left Hand").transform;
            m_RightHand.position = Vector3.zero;
            m_LeftHand.position = new Vector3(50f, 0f, 0f);
            m_RightHand.forward = Vector3.forward;
            m_LeftHand.forward = Vector3.forward;

            m_Quick = m_Root.AddComponent<DesktopQuickInteract>();
            m_Quick.RightHand = m_RightHand;
            m_Quick.LeftHand = m_LeftHand;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_Root);
            Object.Destroy(m_RightHand.gameObject);
            Object.Destroy(m_LeftHand.gameObject);
            Object.Destroy(m_ManagerObject);
        }

        /// <summary>
        /// Creates a cube with a fire alarm on it. Physics does not sync transforms automatically,
        /// so callers must wait for a fixed update before casting a ray at it.
        /// </summary>
        FireAlarm CreateAlarmAt(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "FireAlarm_A";
            go.transform.position = position;
            var alarm = go.AddComponent<FireAlarm>();
            alarm.ObjectId = "FireAlarm_A";
            alarm.ScenarioManager = m_Manager;
            return alarm;
        }

        [UnityTest]
        public IEnumerator TheRightHandKeyUsesWhateverItIsPointingAt()
        {
            var alarm = CreateAlarmAt(new Vector3(0f, 0f, 3f));
            yield return new WaitForFixedUpdate();

            var used = m_Quick.Activate(DesktopQuickInteract.Hand.Right);

            Assert.IsTrue(used);
            Assert.IsTrue(alarm.IsActivated);
            Assert.AreEqual(10, m_Manager.Score);

            Object.Destroy(alarm.gameObject);
        }

        [UnityTest]
        public IEnumerator TheLeftHandKeyUsesWhatTheLeftHandPointsAt()
        {
            var alarm = CreateAlarmAt(new Vector3(50f, 0f, 3f));
            yield return new WaitForFixedUpdate();

            var used = m_Quick.Activate(DesktopQuickInteract.Hand.Left);

            Assert.IsTrue(used);
            Assert.IsTrue(alarm.IsActivated);

            Object.Destroy(alarm.gameObject);
        }

        [UnityTest]
        public IEnumerator PointingAtNothingDoesNothing()
        {
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(m_Quick.Activate(DesktopQuickInteract.Hand.Right));
            Assert.AreEqual(0, m_Manager.Score);
        }

        [UnityTest]
        public IEnumerator ObjectsBeyondTheReachAreIgnored()
        {
            var alarm = CreateAlarmAt(new Vector3(0f, 0f, 40f));
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(m_Quick.Activate(DesktopQuickInteract.Hand.Right));
            Assert.IsFalse(alarm.IsActivated);

            Object.Destroy(alarm.gameObject);
        }

        [UnityTest]
        public IEnumerator ItPressesAUiButtonItIsPointingAt()
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.transform.position = new Vector3(0f, 0f, 3f);
            canvasObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4f, 3f);

            var buttonObject = new GameObject("Start Button");
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 1f);

            var clicked = false;
            button.onClick.AddListener(() => clicked = true);
            yield return null;

            var used = m_Quick.Activate(DesktopQuickInteract.Hand.Right);

            Assert.IsTrue(used);
            Assert.IsTrue(clicked);

            Object.Destroy(canvasObject);
        }

        [UnityTest]
        public IEnumerator ADisabledButtonIsNotPressed()
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.transform.position = new Vector3(0f, 0f, 3f);
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4f, 3f);

            var buttonObject = new GameObject("Hidden Button");
            buttonObject.transform.SetParent(canvasObject.transform, false);
            buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 1f);
            var clicked = false;
            button.onClick.AddListener(() => clicked = true);
            canvasObject.SetActive(false);
            yield return null;

            Assert.IsFalse(m_Quick.Activate(DesktopQuickInteract.Hand.Right));
            Assert.IsFalse(clicked);

            Object.Destroy(canvasObject);
        }
    }
}
