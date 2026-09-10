using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRSim.Interaction;
using VRSim.Scenario;
using VRSim.UI;

namespace VRSim.Tests
{
    /// <summary>
    /// Records the fire evacuation drill as a numbered frame sequence for the demo video.
    ///
    /// The camera is flown along a scripted path while the scenario is played for real: the start
    /// button is pressed, the map and alarm are used, the smoke is entered and left, and the safe
    /// exit is taken. Nothing is faked, so the HUD, score and results shown in the recording are
    /// the ones the running scenario produced.
    ///
    /// Time.captureDeltaTime fixes the timestep, which is what makes an offline recording play
    /// back at a steady rate no matter how long each frame took to render.
    /// </summary>
    [Explicit]
    public class DemoVideoCaptureTests
    {
        const int Width = 1280;
        const int Height = 720;
        const int FramesPerSecond = 30;
        static readonly string OutputFolder = Path.Combine("Build", "DemoFrames");

        ScenarioManager m_Manager;
        ScenarioHud m_Hud;
        Camera m_Camera;
        Transform m_PlayerBody;
        int m_FrameIndex;

        /// <summary>One leg of the camera move, in seconds.</summary>
        readonly struct Leg
        {
            public Leg(float seconds, Vector3 position, Vector3 lookAt, Action onStart = null)
            {
                Seconds = seconds;
                Position = position;
                LookAt = lookAt;
                OnStart = onStart;
            }

            public float Seconds { get; }
            public Vector3 Position { get; }
            public Vector3 LookAt { get; }
            public Action OnStart { get; }
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Directory.Exists(OutputFolder))
                Directory.Delete(OutputFolder, true);
            Directory.CreateDirectory(OutputFolder);

            yield return SceneManager.LoadSceneAsync("FireEvacuation", LoadSceneMode.Single);
            yield return null;

            m_Manager = UnityEngine.Object.FindAnyObjectByType<ScenarioManager>();
            m_Hud = UnityEngine.Object.FindAnyObjectByType<ScenarioHud>();
            m_Camera = Camera.main;

            var body = GameObject.FindGameObjectsWithTag("Player")[0];
            m_PlayerBody = body.transform;
            var follower = body.GetComponent<PlayerBodyFollower>();
            if (follower != null)
                follower.enabled = false;

            // Fixed timestep: the scenario clock advances in even steps regardless of render cost.
            Time.captureDeltaTime = 1f / FramesPerSecond;
            yield return null;
        }

        [TearDown]
        public void TearDown() => Time.captureDeltaTime = 0f;

        [UnityTest]
        public IEnumerator RecordTheFireDrill()
        {
            var start = new Vector3(-7f, 1.65f, -2f);

            var legs = new List<Leg>
            {
                // Read the briefing board.
                new Leg(3.5f, start, start + new Vector3(0f, -0.05f, 3f)),
                new Leg(1.5f, start + new Vector3(0.4f, 0f, 0.3f), start + new Vector3(0f, -0.05f, 3f),
                    () => m_Hud.StartScenario()),

                // Look around the classroom, then head for the door.
                new Leg(3f, new Vector3(-6f, 1.65f, -3f), new Vector3(-2f, 1.2f, -6f)),
                new Leg(3.5f, new Vector3(-3.5f, 1.65f, -2.2f), new Vector3(0f, 1.4f, -1.5f)),

                // Into the hallway, facing the evacuation map.
                new Leg(3.5f, new Vector3(-0.6f, 1.65f, -1.6f), new Vector3(2f, 1.5f, -1f)),
                new Leg(2.5f, new Vector3(0.6f, 1.65f, -1.2f), new Vector3(2f, 1.5f, -1f),
                    () => UnityEngine.Object.FindAnyObjectByType<EvacuationMap>().Interact()),

                // Turn to the alarm on the opposite wall and raise it.
                new Leg(2.5f, new Vector3(0.2f, 1.65f, -1.2f), new Vector3(-2f, 1.4f, -1f)),
                new Leg(2f, new Vector3(-0.6f, 1.65f, -1.1f), new Vector3(-2f, 1.4f, -1f),
                    () => UnityEngine.Object.FindAnyObjectByType<FireAlarm>().Interact()),

                // Look south: the smoke and the barred route.
                new Leg(3f, new Vector3(0f, 1.65f, -2.5f), new Vector3(0f, 1.4f, -9f)),
                new Leg(3.5f, new Vector3(0f, 1.65f, -5.5f), new Vector3(0f, 1.3f, -11f)),

                // Step into the smoke so the warning and the penalty are on camera.
                new Leg(2.5f, new Vector3(0f, 1.65f, -8.2f), new Vector3(0f, 1.3f, -11f)),
                new Leg(2f, new Vector3(0f, 1.65f, -8.2f), new Vector3(0f, 1.4f, -11f)),

                // Back out and turn north towards the lit exit.
                new Leg(3f, new Vector3(0f, 1.65f, -4f), new Vector3(0f, 1.5f, 4f)),
                new Leg(4f, new Vector3(0f, 1.65f, 1f), new Vector3(0f, 1.6f, 8f)),
                new Leg(4f, new Vector3(-0.4f, 1.65f, 6f), new Vector3(0f, 2.2f, 12f)),

                // Through the doorway and out.
                new Leg(3.5f, new Vector3(0f, 1.65f, 11f), new Vector3(0f, 1.5f, 16f)),
                new Leg(2.5f, new Vector3(0f, 1.65f, 13.2f), new Vector3(0f, 1.2f, 17f)),

                // Back to the board for the results.
                new Leg(1.5f, start + new Vector3(0f, 0f, 0.6f), start + new Vector3(0f, -0.05f, 3f)),
                new Leg(5f, start, start + new Vector3(0f, -0.05f, 3f)),
            };

            var previousPosition = legs[0].Position;

            foreach (var leg in legs)
            {
                leg.OnStart?.Invoke();

                var frames = Mathf.RoundToInt(leg.Seconds * FramesPerSecond);
                for (var frame = 0; frame < frames; frame++)
                {
                    // Smoothstep between waypoints so the camera eases rather than snapping.
                    var t = frames <= 1 ? 1f : frame / (float)(frames - 1);
                    var eased = t * t * (3f - 2f * t);
                    var position = Vector3.Lerp(previousPosition, leg.Position, eased);

                    Place(position, leg.LookAt);

                    yield return new WaitForFixedUpdate();
                    yield return null;

                    CaptureFrame();
                }

                previousPosition = leg.Position;
            }

            Assert.Greater(m_FrameIndex, 0, "no frames were recorded");
            Debug.Log($"[Demo] wrote {m_FrameIndex} frames at {FramesPerSecond} fps " +
                      $"({m_FrameIndex / (float)FramesPerSecond:0.0} seconds)");
        }

        void Place(Vector3 position, Vector3 lookAt)
        {
            var rig = m_Camera.transform.root;
            rig.position = new Vector3(position.x, 0f, position.z);
            m_Camera.transform.position = position;

            var direction = lookAt - position;
            if (direction.sqrMagnitude > 0.0001f)
                m_Camera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            m_PlayerBody.position = new Vector3(position.x, 0f, position.z);
        }

        void CaptureFrame()
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

            var path = Path.Combine(OutputFolder, $"frame_{m_FrameIndex:D5}.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            m_FrameIndex++;

            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
