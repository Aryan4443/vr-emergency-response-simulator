using System.IO;
using System.Linq;
using NUnit.Framework;
using VRSim.Evaluation;

namespace VRSim.Tests
{
    public class ScenarioResultStoreTests
    {
        string m_Folder;

        [SetUp]
        public void SetUp()
        {
            m_Folder = Path.Combine(Path.GetTempPath(), "vrsim-results-" + Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Folder))
                Directory.Delete(m_Folder, true);
        }

        static ScenarioResult SampleResult(int score) => new ScenarioResult
        {
            scenarioId = "fire_evacuation_01",
            completed = true,
            completionTimeSeconds = 92.4f,
            unsafeZoneEntries = 1,
            incorrectActions = 1,
            selectedExit = "north_exit",
            score = score,
        };

        [Test]
        public void CreatesTheFolderOnFirstSave()
        {
            var store = new ScenarioResultStore(m_Folder);

            store.Save(SampleResult(78));

            Assert.IsTrue(Directory.Exists(m_Folder));
        }

        [Test]
        public void WritesOneJsonFilePerRun()
        {
            var store = new ScenarioResultStore(m_Folder);

            store.Save(SampleResult(78));
            store.Save(SampleResult(64));

            Assert.AreEqual(2, Directory.GetFiles(m_Folder, "*.json").Length);
        }

        [Test]
        public void SavedFileCanBeReadBack()
        {
            var store = new ScenarioResultStore(m_Folder);

            var path = store.Save(SampleResult(78));
            var restored = JsonUtilityBridge.FromJson(File.ReadAllText(path));

            Assert.AreEqual("fire_evacuation_01", restored.scenarioId);
            Assert.AreEqual(78, restored.score);
        }

        [Test]
        public void LoadAllReturnsEveryStoredRun()
        {
            var store = new ScenarioResultStore(m_Folder);
            store.Save(SampleResult(78));
            store.Save(SampleResult(64));

            var all = store.LoadAll();

            Assert.AreEqual(2, all.Count);
            CollectionAssert.AreEquivalent(new[] { 78, 64 }, new[] { all[0].score, all[1].score });
        }

        [Test]
        public void LoadAllReturnsRunsOldestFirst()
        {
            var store = new ScenarioResultStore(m_Folder);

            // Written in a known order; the file names carry the timestamp that defines it.
            var scores = new[] { 10, 20, 30, 40, 50 };
            foreach (var score in scores)
                store.Save(SampleResult(score));

            var loaded = store.LoadAll().Select(r => r.score).ToArray();

            CollectionAssert.AreEqual(scores, loaded,
                "runs must come back in the order they were recorded");
        }

        [Test]
        public void SavingStampsTheRunWithTheTimeItWasRecorded()
        {
            var store = new ScenarioResultStore(m_Folder);
            var result = SampleResult(78);

            store.Save(result);

            Assert.Greater(result.recordedAtUtcTicks, 0L);
        }

        [Test]
        public void TwoRunsSavedInTheSameMillisecondStillGetDistinctTimestamps()
        {
            var store = new ScenarioResultStore(m_Folder);
            var first = SampleResult(10);
            var second = SampleResult(20);

            store.Save(first);
            store.Save(second);

            Assert.Less(first.recordedAtUtcTicks, second.recordedAtUtcTicks);
        }

        [Test]
        public void LoadAllReturnsNothingWhenNoRunsAreStored()
        {
            var store = new ScenarioResultStore(m_Folder);

            Assert.AreEqual(0, store.LoadAll().Count);
        }
    }

    static class JsonUtilityBridge
    {
        public static ScenarioResult FromJson(string json) =>
            UnityEngine.JsonUtility.FromJson<ScenarioResult>(json);
    }
}
