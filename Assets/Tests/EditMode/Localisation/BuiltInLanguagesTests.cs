using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using VRSim.Evaluation;
using VRSim.Scenario;
using VRSim.Localisation;

namespace VRSim.Tests
{
    public class BuiltInLanguagesTests
    {
        static readonly string[] k_GuidanceKeys =
        {
            Localiser.Keys.GuidanceFollowExitSigns,
            Localiser.Keys.GuidanceAvoidSmoke,
            Localiser.Keys.GuidanceReadMap,
            Localiser.Keys.GuidanceRaiseAlarm,
            Localiser.Keys.GuidanceCheckDoors,
            Localiser.Keys.GuidanceCleanRun,
        };

        // Keys whose wording is a symbol or a pure placeholder, so the same text is correct in
        // every language and an identical English and Spanish value is not a missed translation.
        static readonly string[] k_UntranslatedByDesign =
        {
            Localiser.Keys.ReportSummaryNoExit,
            Localiser.Keys.ObjectiveProgress,
        };

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
            return result;
        }

        static IEnumerable<string> Placeholders(string text) =>
            Regex.Matches(text, @"\{\d+\}")
                .Cast<Match>()
                .Select(m => m.Value)
                .Distinct()
                .OrderBy(v => v, System.StringComparer.Ordinal);

        [Test]
        public void EnglishAndSpanishContainExactlyTheSameKeys()
        {
            var english = BuiltInLanguages.English().Keys.ToList();
            var spanish = BuiltInLanguages.Spanish().Keys.ToList();

            // The test that catches a translator dropping a line: a language that is missing a key
            // would otherwise silently show a raw identifier to the trainee who needs it most.
            var untranslated = english.Except(spanish).ToList();
            var unexpected = spanish.Except(english).ToList();

            CollectionAssert.IsEmpty(untranslated,
                "Spanish is missing: " + string.Join(", ", untranslated));
            CollectionAssert.IsEmpty(unexpected,
                "Spanish has keys English does not: " + string.Join(", ", unexpected));
            Assert.AreEqual(english.Count, spanish.Count, "a key is duplicated in one of the tables");
        }

        [Test]
        public void BothBuiltInLanguagesCoverEveryDeclaredKey()
        {
            foreach (var table in BuiltInLanguages.All())
            {
                foreach (var key in Localiser.Keys.All)
                    Assert.IsTrue(table.Has(key), table.LanguageCode + " is missing " + key);

                Assert.AreEqual(Localiser.Keys.All.Count, table.Count,
                    table.LanguageCode + " carries strings that are not declared in Keys");
            }
        }

        [Test]
        public void TheDeclaredKeyListMatchesTheConstantsAndHasNoDuplicates()
        {
            var declared = typeof(Localiser.Keys)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();

            CollectionAssert.AreEquivalent(declared, Localiser.Keys.All.ToList(),
                "Keys.All has fallen out of step with the constants beside it");
            Assert.AreEqual(declared.Count, declared.Distinct().Count(),
                "two key constants share the same string, so one silently overwrites the other");
        }

        [Test]
        public void EnglishRepeatsTheDisclaimerWordForWord()
        {
            Assert.AreEqual(ScenarioReport.Disclaimer,
                BuiltInLanguages.English().Get(Localiser.Keys.ReportDisclaimer));
        }

        [Test]
        public void EnglishRepeatsBothResultsHeadlines()
        {
            var english = BuiltInLanguages.English();
            var result = PerfectRun();

            Assert.AreEqual(ScenarioReport.Headline(result),
                english.Get(Localiser.Keys.ReportHeadlineSuccess));

            result.completed = false;
            Assert.AreEqual(ScenarioReport.Headline(result),
                english.Get(Localiser.Keys.ReportHeadlineFailure));
        }

        [Test]
        public void EnglishSummaryLinesReadExactlyAsTheResultsScreenDoesToday()
        {
            var localisation = BuiltInLanguages.CreateDefault();
            var result = PerfectRun();
            var lines = ScenarioReport.SummaryLines(result).ToList();

            CollectionAssert.Contains(lines, localisation.Get(Localiser.Keys.ReportSummaryTime,
                ScenarioReport.FormatTime(result.completionTimeSeconds)));
            CollectionAssert.Contains(lines, localisation.Get(Localiser.Keys.ReportSummaryUnsafeAreas,
                result.unsafeZoneEntries));
            CollectionAssert.Contains(lines, localisation.Get(Localiser.Keys.ReportSummaryIncorrectActions,
                result.incorrectActions));
            CollectionAssert.Contains(lines, localisation.Get(Localiser.Keys.ReportSummaryExitTaken,
                result.selectedExit));
            CollectionAssert.Contains(lines, localisation.Get(Localiser.Keys.ReportSummaryScore,
                result.score));
        }

        [Test]
        public void EnglishCarriesTheDashShownWhenNoExitWasReached()
        {
            var result = PerfectRun();
            result.completed = false;
            result.selectedExit = null;

            var lines = ScenarioReport.SummaryLines(result).ToList();
            var localisation = BuiltInLanguages.CreateDefault();
            var noExit = localisation.Get(Localiser.Keys.ReportSummaryNoExit);

            CollectionAssert.Contains(lines,
                localisation.Get(Localiser.Keys.ReportSummaryExitTaken, noExit));
        }

        [Test]
        public void EnglishCarriesEveryPieceOfImprovementGuidance()
        {
            var english = BuiltInLanguages.English();
            var localisedGuidance = k_GuidanceKeys.Select(english.Get).ToList();

            var poorRun = new ScenarioResult
            {
                completed = false,
                unsafeZoneEntries = 2,
                incorrectActions = 1,
            };

            foreach (var line in ScenarioReport.Guidance(poorRun))
                CollectionAssert.Contains(localisedGuidance, line);

            foreach (var line in ScenarioReport.Guidance(PerfectRun()))
                CollectionAssert.Contains(localisedGuidance, line);
        }

        [Test]
        public void EnglishObjectiveTextMatchesTheFireDrillChecklist()
        {
            var english = BuiltInLanguages.English();

            var titles = new Dictionary<string, string>
            {
                { "read_map", Localiser.Keys.ObjectiveReadMapTitle },
                { "raise_alarm", Localiser.Keys.ObjectiveRaiseAlarmTitle },
                { "avoid_hazard", Localiser.Keys.ObjectiveAvoidHazardTitle },
                { "reach_exit", Localiser.Keys.ObjectiveReachExitTitle },
            };

            var hints = new Dictionary<string, string>
            {
                { "read_map", Localiser.Keys.ObjectiveReadMapHint },
                { "raise_alarm", Localiser.Keys.ObjectiveRaiseAlarmHint },
                { "avoid_hazard", Localiser.Keys.ObjectiveAvoidHazardHint },
                { "reach_exit", Localiser.Keys.ObjectiveReachExitHint },
            };

            var objectives = ObjectiveTracker.CreateFireEvacuationObjectives().Objectives;
            Assert.AreEqual(titles.Count, objectives.Count,
                "the drill has gained or lost a step that the string table does not know about");

            foreach (var objective in objectives)
            {
                Assert.AreEqual(objective.Title, english.Get(titles[objective.Id]));
                Assert.AreEqual(objective.Hint, english.Get(hints[objective.Id]));
            }
        }

        [Test]
        public void EnglishFormatsTheObjectiveProgressTheSameWayTheHudDoes()
        {
            var tracker = ObjectiveTracker.CreateFireEvacuationObjectives();
            tracker.Complete("read_map");
            tracker.Complete("raise_alarm");

            var localisation = BuiltInLanguages.CreateDefault();

            Assert.AreEqual(tracker.Progress,
                localisation.Get(Localiser.Keys.ObjectiveProgress,
                    tracker.CompletedCount, tracker.Objectives.Count));
        }

        [Test]
        public void SpanishIsGenuinelyTranslatedRatherThanCopied()
        {
            var english = BuiltInLanguages.English();
            var spanish = BuiltInLanguages.Spanish();

            Assert.AreEqual("es", spanish.LanguageCode);
            Assert.IsNotEmpty(spanish.DisplayName);

            foreach (var key in Localiser.Keys.All.Except(k_UntranslatedByDesign))
            {
                Assert.AreNotEqual(english.Get(key), spanish.Get(key),
                    key + " is still in English");
                Assert.IsNotEmpty(spanish.Get(key), key + " has no Spanish wording");
            }
        }

        [Test]
        public void SpanishKeepsEveryPlaceholderItsEnglishLineUses()
        {
            var english = BuiltInLanguages.English();
            var spanish = BuiltInLanguages.Spanish();

            // A dropped "{0}" would leave the figure off the results screen entirely.
            foreach (var key in Localiser.Keys.All)
            {
                CollectionAssert.AreEqual(
                    Placeholders(english.Get(key)).ToList(),
                    Placeholders(spanish.Get(key)).ToList(),
                    key + " does not use the same placeholders in both languages");
            }
        }

        [Test]
        public void BothBuiltInTablesSurviveAJsonRoundTrip()
        {
            foreach (var table in BuiltInLanguages.All())
            {
                var restored = LocalisedStringTable.FromJson(table.ToJson());

                Assert.AreEqual(table.LanguageCode, restored.LanguageCode);
                Assert.AreEqual(table.DisplayName, restored.DisplayName);
                CollectionAssert.AreEqual(table.Keys.ToList(), restored.Keys.ToList());

                foreach (var key in Localiser.Keys.All)
                    Assert.AreEqual(table.Get(key), restored.Get(key), key + " changed on reload");
            }
        }

        [Test]
        public void TheDefaultLookupOffersBothLanguagesAndStartsInEnglish()
        {
            var localisation = BuiltInLanguages.CreateDefault();

            CollectionAssert.AreEqual(
                new[] { BuiltInLanguages.EnglishCode, BuiltInLanguages.SpanishCode },
                localisation.AvailableLanguages.ToList());
            Assert.AreEqual(BuiltInLanguages.EnglishCode, localisation.CurrentLanguage);
            Assert.AreEqual(ScenarioReport.Disclaimer,
                localisation.Get(Localiser.Keys.ReportDisclaimer));
        }

        [Test]
        public void RunningTheWholeKeySetThroughBothLanguagesLeavesNoGaps()
        {
            var localisation = BuiltInLanguages.CreateDefault();

            foreach (var code in localisation.AvailableLanguages)
            {
                Assert.IsTrue(localisation.SetLanguage(code));
                foreach (var key in Localiser.Keys.All)
                    Assert.AreNotEqual(key, localisation.Get(key), key + " is untranslated in " + code);
            }

            CollectionAssert.IsEmpty(localisation.MissingKeys);
        }
    }
}
