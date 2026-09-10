using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRSim.Interaction;

namespace VRSim.Tests
{
    /// <summary>
    /// Renders the earthquake level so it can be reviewed without a headset. Explicit because it
    /// writes files; run it with Tools/capture-earthquake.sh.
    /// </summary>
    [Explicit]
    public class EarthquakeSceneCaptureTests
    {
        const int Width = 1280;
        const int Height = 720;
        static readonly string OutputFolder = Path.Combine("Build", "CaptureEarthquake");

        Camera m_Camera;
        Transform m_PlayerBody;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Directory.CreateDirectory(OutputFolder);
            yield return SceneManager.LoadSceneAsync("Earthquake", LoadSceneMode.Single);
            yield return null;

            m_Camera = Camera.main;
            Assert.IsNotNull(m_Camera, "the earthquake scene has no camera");

            var body = GameObject.FindGameObjectsWithTag("Player")[0];
            m_PlayerBody = body.transform;
            var follower = body.GetComponent<PlayerBodyFollower>();
            if (follower != null)
                follower.enabled = false;

            yield return null;
        }

        IEnumerator Shot(string label, Vector3 position, Vector3 lookDirection)
        {
            var rig = m_Camera.transform.root;
            rig.position = new Vector3(position.x, 0f, position.z);
            m_Camera.transform.position = position;
            m_Camera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            m_PlayerBody.position = new Vector3(position.x, 0f, position.z);

            yield return new WaitForFixedUpdate();
            yield return null;

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

            File.WriteAllBytes(Path.Combine(OutputFolder, label + ".png"), image.EncodeToPNG());
            Debug.Log($"[Capture] wrote {label}");

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
        }

        [UnityTest]
        public IEnumerator CaptureTheEarthquakeLevel()
        {
            yield return Shot("01-classroom", new Vector3(0f, 1.7f, -7.5f), new Vector3(0.2f, -0.05f, 1f));
            yield return Shot("02-glazing", new Vector3(2f, 1.7f, -4f), new Vector3(1f, -0.1f, 0.15f));
            yield return Shot("03-shelving", new Vector3(-3f, 1.7f, -5f), new Vector3(-1f, -0.05f, -0.2f));
            yield return Shot("04-under-desk", new Vector3(0f, 0.7f, -4.6f), new Vector3(0f, 0.1f, 1f));
            yield return Shot("05-corridor", new Vector3(0f, 1.7f, 4f), new Vector3(0f, -0.05f, 1f));
            yield return Shot("06-stairs-and-lift", new Vector3(0f, 1.7f, 10f), new Vector3(0f, -0.05f, 1f));
            yield return Shot("07-assembly", new Vector3(-1.1f, 1.7f, 17f), new Vector3(0f, -0.1f, 1f));
            yield return Shot("08-overview", new Vector3(0f, 26f, 0f), new Vector3(0f, -1f, 0.35f));

            Assert.AreEqual(8, Directory.GetFiles(OutputFolder, "*.png").Length);
        }
    }
}
