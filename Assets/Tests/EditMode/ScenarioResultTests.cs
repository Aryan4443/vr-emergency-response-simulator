using NUnit.Framework;
using UnityEngine;
using VRSim.Evaluation;

namespace VRSim.Tests
{
    public class ScenarioResultTests
    {
        [Test]
        public void SerialisesTheFieldNamesFromTheSpecification()
        {
            var result = new ScenarioResult
            {
                scenarioId = "fire_evacuation_01",
                completed = true,
                completionTimeSeconds = 92.4f,
                unsafeZoneEntries = 1,
                incorrectActions = 1,
                selectedExit = "north_exit",
                score = 78,
            };

            var json = JsonUtility.ToJson(result);

            StringAssert.Contains("\"scenarioId\":\"fire_evacuation_01\"", json);
            StringAssert.Contains("\"completed\":true", json);
            StringAssert.Contains("\"unsafeZoneEntries\":1", json);
            StringAssert.Contains("\"incorrectActions\":1", json);
            StringAssert.Contains("\"selectedExit\":\"north_exit\"", json);
            StringAssert.Contains("\"score\":78", json);
        }

        [Test]
        public void SurvivesAJsonRoundTrip()
        {
            var result = new ScenarioResult
            {
                scenarioId = "fire_evacuation_01",
                completed = true,
                completionTimeSeconds = 92.4f,
                unsafeZoneEntries = 2,
                incorrectActions = 3,
                selectedExit = "north_exit",
                score = 78,
            };
            result.events.Add(new ActionEvent
            {
                actionType = "alarm_activated",
                objectId = "FireAlarm_A",
                timestampSeconds = 12.5f,
                successful = true,
            });

            var restored = JsonUtility.FromJson<ScenarioResult>(JsonUtility.ToJson(result));

            Assert.AreEqual("fire_evacuation_01", restored.scenarioId);
            Assert.IsTrue(restored.completed);
            Assert.AreEqual(92.4f, restored.completionTimeSeconds, 0.001f);
            Assert.AreEqual(2, restored.unsafeZoneEntries);
            Assert.AreEqual(3, restored.incorrectActions);
            Assert.AreEqual("north_exit", restored.selectedExit);
            Assert.AreEqual(78, restored.score);
            Assert.AreEqual(1, restored.events.Count);
            Assert.AreEqual("alarm_activated", restored.events[0].actionType);
            Assert.AreEqual("FireAlarm_A", restored.events[0].objectId);
            Assert.AreEqual(12.5f, restored.events[0].timestampSeconds, 0.001f);
            Assert.IsTrue(restored.events[0].successful);
        }

        [Test]
        public void CarriesNoIdentifyingInformation()
        {
            var fields = typeof(ScenarioResult).GetFields();

            foreach (var field in fields)
            {
                var name = field.Name.ToLowerInvariant();
                Assert.IsFalse(name.Contains("name"), $"{field.Name} looks identifying");
                Assert.IsFalse(name.Contains("user"), $"{field.Name} looks identifying");
                Assert.IsFalse(name.Contains("email"), $"{field.Name} looks identifying");
                Assert.IsFalse(name.Contains("device"), $"{field.Name} looks identifying");
            }
        }
    }
}
