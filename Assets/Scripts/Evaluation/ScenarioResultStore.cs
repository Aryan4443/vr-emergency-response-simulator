using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VRSim.Evaluation
{
    /// <summary>
    /// Stores anonymous scenario results locally as JSON, as described in section 11.
    /// One file per run keeps a partly written file from destroying earlier runs.
    /// </summary>
    public class ScenarioResultStore
    {
        readonly string m_Folder;

        /// <summary>
        /// Guards against two runs being stamped with the same tick. UtcNow does not advance on
        /// every call, so a rapid sequence of saves would otherwise be unorderable.
        /// </summary>
        static long s_LastStampedTicks;

        /// <param name="folder">
        /// Destination folder. Defaults to a "Results" folder inside the application's
        /// persistent data path, which is writable on both the editor and the headset.
        /// </param>
        public ScenarioResultStore(string folder = null)
        {
            m_Folder = folder ?? Path.Combine(Application.persistentDataPath, "Results");
        }

        /// <summary>Writes one run and returns the path it was written to.</summary>
        public string Save(ScenarioResult result)
        {
            Directory.CreateDirectory(m_Folder);

            result.recordedAtUtcTicks = NextTimestamp();

            // The timestamp alone collides when two runs are saved inside the same millisecond,
            // so a short random suffix keeps every run on its own file.
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var path = Path.Combine(m_Folder, $"{result.scenarioId}-{stamp}-{suffix}.json");

            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            return path;
        }

        /// <summary>A UTC tick count guaranteed to be greater than the previous one.</summary>
        static long NextTimestamp()
        {
            var now = DateTime.UtcNow.Ticks;
            var stamped = now > s_LastStampedTicks ? now : s_LastStampedTicks + 1;
            s_LastStampedTicks = stamped;
            return stamped;
        }

        /// <summary>
        /// Reads every stored run, oldest first. Unreadable files are skipped rather than thrown.
        ///
        /// The order matters: anything comparing performance across sessions is meaningless if the
        /// runs arrive shuffled, and Directory.GetFiles gives no ordering guarantee. File names
        /// begin with the scenario id followed by a UTC timestamp, so an ordinal sort puts a
        /// scenario's runs in the order they happened.
        /// </summary>
        public IReadOnlyList<ScenarioResult> LoadAll()
        {
            var results = new List<ScenarioResult>();
            if (!Directory.Exists(m_Folder))
                return results;

            foreach (var path in Directory.GetFiles(m_Folder, "*.json"))
            {
                try
                {
                    var result = JsonUtility.FromJson<ScenarioResult>(File.ReadAllText(path));
                    if (result != null)
                        results.Add(result);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Skipped unreadable result file {path}: {exception.Message}");
                }
            }

            // Ordered by the time carried in each record. Results written before that field existed
            // report zero and stay at the front, which keeps them in the history rather than
            // discarding them.
            return results.OrderBy(r => r.recordedAtUtcTicks).ToList();
        }
    }
}
