using System.Linq;
using NUnit.Framework;
using VRSim.Evaluation;

namespace VRSim.Tests
{
    public class ScenarioReportTests
    {
        static ScenarioResult PerfectRun()
        {
            var result = new ScenarioResult
            {
                scenarioId = "fire_evacuation_01",
                completed = true,
                completionTimeSeconds = 92.4f,
                unsafeZoneEntries = 0,
                incorrectActions = 0,
                selectedExit = "north_exit",
                score = 85,
            };
            result.events.Add(new ActionEvent { actionType = "map_viewed", successful = true });
            result.events.Add(new ActionEvent { actionType = "alarm_activated", successful = true });
            result.events.Add(new ActionEvent { actionType = "safe_exit_reached", successful = true });
            return result;
        }

        [Test]
        public void FormatsTimeAsMinutesAndSeconds()
        {
            Assert.AreEqual("0:00", ScenarioReport.FormatTime(0f));
            Assert.AreEqual("0:09", ScenarioReport.FormatTime(9.4f));
            Assert.AreEqual("1:32", ScenarioReport.FormatTime(92.4f));
            Assert.AreEqual("10:05", ScenarioReport.FormatTime(605f));
        }

        [Test]
        public void NegativeTimeIsShownAsZero()
        {
            Assert.AreEqual("0:00", ScenarioReport.FormatTime(-3f));
        }

        [Test]
        public void HeadlineSaysWhetherTheUserEvacuatedSafely()
        {
            Assert.AreEqual("Evacuated safely", ScenarioReport.Headline(PerfectRun()));

            var failed = PerfectRun();
            failed.completed = false;
            Assert.AreEqual("Did not reach a safe exit", ScenarioReport.Headline(failed));
        }

        [Test]
        public void SummaryReportsEverySectionFourPointSixField()
        {
            var lines = ScenarioReport.SummaryLines(PerfectRun()).ToList();

            Assert.IsTrue(lines.Any(l => l.StartsWith("Time:") && l.Contains("1:32")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Unsafe areas entered:") && l.Contains("0")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Incorrect actions:") && l.Contains("0")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Exit taken:") && l.Contains("north_exit")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Score:") && l.Contains("85")));
        }

        [Test]
        public void SummaryShowsADashWhenNoExitWasReached()
        {
            var result = PerfectRun();
            result.completed = false;
            result.selectedExit = null;

            var lines = ScenarioReport.SummaryLines(result).ToList();

            Assert.IsTrue(lines.Any(l => l.StartsWith("Exit taken:") && l.Contains("—")));
        }

        [Test]
        public void APerfectRunIsToldWhatItDidWell()
        {
            var guidance = ScenarioReport.Guidance(PerfectRun()).ToList();

            Assert.AreEqual(1, guidance.Count);
            StringAssert.Contains("alarm", guidance[0].ToLowerInvariant());
        }

        [Test]
        public void EnteringSmokeEarnsGuidanceAboutUnsafeAreas()
        {
            var result = PerfectRun();
            result.unsafeZoneEntries = 2;

            var guidance = ScenarioReport.Guidance(result).ToList();

            Assert.IsTrue(guidance.Any(g => g.ToLowerInvariant().Contains("smoke")));
        }

        [Test]
        public void SkippingTheMapEarnsGuidanceAboutPlanningTheRoute()
        {
            var result = PerfectRun();
            result.events.RemoveAll(e => e.actionType == "map_viewed");

            var guidance = ScenarioReport.Guidance(result).ToList();

            Assert.IsTrue(guidance.Any(g => g.ToLowerInvariant().Contains("evacuation map")));
        }

        [Test]
        public void SkippingTheAlarmEarnsGuidanceAboutWarningOthers()
        {
            var result = PerfectRun();
            result.events.RemoveAll(e => e.actionType == "alarm_activated");

            var guidance = ScenarioReport.Guidance(result).ToList();

            Assert.IsTrue(guidance.Any(g => g.ToLowerInvariant().Contains("alarm")));
        }

        [Test]
        public void AnIncompleteRunIsToldToFollowTheExitSigns()
        {
            var result = PerfectRun();
            result.completed = false;
            result.selectedExit = null;

            var guidance = ScenarioReport.Guidance(result).ToList();

            Assert.IsTrue(guidance.Any(g => g.ToLowerInvariant().Contains("exit sign")));
        }

        [Test]
        public void TheDisclaimerMakesClearThisIsNotCertifiedTraining()
        {
            var disclaimer = ScenarioReport.Disclaimer.ToLowerInvariant();

            StringAssert.Contains("educational", disclaimer);
            StringAssert.Contains("not", disclaimer);
            Assert.IsTrue(disclaimer.Contains("certified") || disclaimer.Contains("certification"));
        }
    }
}
