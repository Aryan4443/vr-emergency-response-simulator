using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRSim.Interaction;
using VRSim.Scenario;

namespace VRSim.Tests
{
    /// <summary>
    /// Plays the fire evacuation drill and renders a frame at each step, so the scenario can be
    /// reviewed as images without a headset.
    ///
    /// Marked Explicit because it writes files and is slower than the behavioural tests. Run it on
    /// its own with:
    ///   ./Tools/capture.sh
    /// </summary>
    [Explicit]
    public class SceneCaptureTests
    {
        const int Width = 1280;
        const int Height = 720;
        static readonly string OutputFolder = Path.Combine("Build", "Capture");

        ScenarioManager m_Manager;
        Transform m_PlayerBody;
        Camera m_Camera;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Directory.CreateDirectory(OutputFolder);

            yield return SceneManager.LoadSceneAsync("FireEvacuation", LoadSceneMode.Single);
            yield return null;

            m_Manager = Object.FindAnyObjectByType<ScenarioManager>();
            m_Camera = Camera.main;
            Assert.IsNotNull(m_Camera, "the scene has no camera");

            var body = GameObject.FindGameObjectsWithTag("Player")[0];
            m_PlayerBody = body.transform;
            var follower = body.GetComponent<PlayerBodyFollower>();
            if (follower != null)
                follower.enabled = false;

            yield return null;
        }

        /// <summary>Moves the head and the collider together, then renders a frame.</summary>
        IEnumerator Shot(string label, Vector3 position, Vector3 lookDirection)
        {
            var rig = m_Camera.transform.root;
            rig.position = new Vector3(position.x, 0f, position.z);
            m_Camera.transform.position = position;
            m_Camera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            m_PlayerBody.position = new Vector3(position.x, 0f, position.z);

            // WaitForEndOfFrame never resumes in batch mode, because there is no render loop to
            // end. Camera.Render below draws explicitly, so waiting for a frame is unnecessary.
            yield return new WaitForFixedUpdate();
            yield return null;

            Capture(label);
        }

        void Capture(string label)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previous = m_Camera.targetTexture;
            var activeBefore = RenderTexture.active;

            m_Camera.targetTexture = target;
            m_Camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();

            m_Camera.targetTexture = previous;
            RenderTexture.active = activeBefore;

            var path = Path.Combine(OutputFolder, label + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log($"[Capture] wrote {path}");

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
        }

        [UnityTest]
        public IEnumerator CaptureTheWholeDrill()
        {
            // 1. Briefing, as the user sees it on spawning in the west room.
            yield return Shot("01-briefing", new Vector3(-7f, 1.7f, -2f), Vector3.forward);

            m_Manager.BeginBriefing();
            m_Manager.StartScenario();
            yield return null;

            // 2. The classroom the drill starts in.
            yield return Shot("02-start-room", new Vector3(-9f, 1.7f, -3.5f), new Vector3(1f, -0.1f, 0.4f));

            // 3. Hallway with the alarm and the map facing each other.
            yield return Shot("03-hallway", new Vector3(0f, 1.7f, -4f), new Vector3(0f, -0.05f, 1f));

            Object.FindAnyObjectByType<EvacuationMap>().Interact();
            Object.FindAnyObjectByType<FireAlarm>().Interact();
            yield return null;

            // 4. Alarm raised and map read: the HUD shows the running score.
            yield return Shot("04-alarm-raised", new Vector3(-1.2f, 1.6f, -1f), new Vector3(-1f, -0.2f, 0f));

            // 5. The smoke blocking the south route, seen from a safe distance.
            yield return Shot("05-smoke-ahead", new Vector3(0f, 1.7f, -4.5f), new Vector3(0f, -0.1f, -1f));

            // 6. Standing in the smoke: warning prompt and the score penalty.
            yield return Shot("06-in-smoke", new Vector3(0f, 1.7f, -8.5f), new Vector3(0f, -0.1f, -1f));

            // Back out of the hazard and take the safe route north.
            yield return Shot("07-back-to-safety", new Vector3(0f, 1.7f, -2f), new Vector3(0f, -0.05f, 1f));
            yield return Shot("08-north-exit-sign", new Vector3(0f, 1.7f, 8f), new Vector3(0f, 0.05f, 1f));

            // 9. Through the exit, which ends the run and opens the results board.
            yield return Shot("09-evacuated", new Vector3(0f, 1.7f, 13f), new Vector3(0f, -0.05f, 1f));
            yield return null;
            yield return null;

            // 10. The results board back at the start position.
            yield return Shot("10-results", new Vector3(-7f, 1.7f, -2f), Vector3.forward);

            // 11. A plan view of the whole building for the README.
            yield return Shot("11-overview", new Vector3(0f, 22f, -2f), new Vector3(0f, -1f, 0.35f));

            Assert.AreEqual(ScenarioState.Review, m_Manager.State);
            Assert.AreEqual(11, Directory.GetFiles(OutputFolder, "*.png").Length);
        }
    }
}
