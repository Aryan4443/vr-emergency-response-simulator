using System.Globalization;
using System.Linq;
using NUnit.Framework;
using VRSim.Localisation;

namespace VRSim.Tests
{
    public class LocalisationTests
    {
        static LocalisedStringTable EnglishSample()
        {
            var table = new LocalisedStringTable("en", "English");
            table.Set("headline", "Evacuated safely");
            table.Set("score", "Score: {0}");
            table.Set("progress", "{0}/{1}");
            return table;
        }

        static LocalisedStringTable SpanishSample()
        {
            var table = new LocalisedStringTable("es", "Espanol");
            table.Set("headline", "Evacuación segura");
            table.Set("score", "Puntuación: {0}");
            table.Set("progress", "{0}/{1}");
            return table;
        }

        static Localiser TwoLanguages()
        {
            var localisation = new Localiser();
            localisation.Register(EnglishSample());
            localisation.Register(SpanishSample());
            return localisation;
        }

        [Test]
        public void AnEmptyLookupStillReturnsSomethingDisplayable()
        {
            var localisation = new Localiser();

            Assert.AreEqual(string.Empty, localisation.CurrentLanguage);
            Assert.AreEqual(0, localisation.AvailableLanguages.Count);
            Assert.AreEqual("headline", localisation.Get("headline"));
        }

        [Test]
        public void TheFirstRegisteredLanguageBecomesTheCurrentOne()
        {
            var localisation = new Localiser();

            Assert.IsTrue(localisation.Register(EnglishSample()));

            Assert.AreEqual("en", localisation.CurrentLanguage);
            Assert.AreEqual("English", localisation.CurrentDisplayName);
        }

        [Test]
        public void RegisteringLaterLanguagesDoesNotChangeTheSelection()
        {
            var localisation = TwoLanguages();

            Assert.AreEqual("en", localisation.CurrentLanguage);
        }

        [Test]
        public void AnUnusableTableIsRefused()
        {
            var localisation = new Localiser();

            Assert.IsFalse(localisation.Register(null));
            Assert.IsFalse(localisation.Register(LocalisedStringTable.FromJson("{ corrupt")),
                "a table recovered from a corrupt file must not shadow a real language");
            Assert.AreEqual(0, localisation.AvailableLanguages.Count);
        }

        [Test]
        public void RegisteringTheSameCodeReplacesTheTableInPlace()
        {
            var localisation = TwoLanguages();

            var replacement = new LocalisedStringTable("en", "English");
            replacement.Set("headline", "Everyone out");
            Assert.IsTrue(localisation.Register(replacement));

            Assert.AreEqual(2, localisation.AvailableLanguages.Count);
            Assert.AreEqual("en", localisation.CurrentLanguage);
            Assert.AreEqual("Everyone out", localisation.Get("headline"));
        }

        [Test]
        public void AvailableLanguagesListsEveryCodeInRegistrationOrder()
        {
            CollectionAssert.AreEqual(new[] { "en", "es" }, TwoLanguages().AvailableLanguages.ToList());
        }

        [Test]
        public void SwitchingLanguageChangesTheWording()
        {
            var localisation = TwoLanguages();

            Assert.AreEqual("Evacuated safely", localisation.Get("headline"));

            Assert.IsTrue(localisation.SetLanguage("es"));

            Assert.AreEqual("es", localisation.CurrentLanguage);
            Assert.AreEqual("Evacuación segura", localisation.Get("headline"));
        }

        [Test]
        public void LanguageCodesAreMatchedCaseInsensitively()
        {
            var localisation = TwoLanguages();

            Assert.IsTrue(localisation.SetLanguage("ES"));
            Assert.AreEqual("es", localisation.CurrentLanguage);
            Assert.IsTrue(localisation.HasLanguage("En"));
        }

        [Test]
        public void AnUnknownLanguageIsRejectedAndTheCurrentOneIsKept()
        {
            var localisation = TwoLanguages();

            Assert.IsFalse(localisation.SetLanguage("fr"));
            Assert.IsFalse(localisation.SetLanguage(null));
            Assert.IsFalse(localisation.SetLanguage(string.Empty));

            Assert.AreEqual("en", localisation.CurrentLanguage);
            Assert.AreEqual("Evacuated safely", localisation.Get("headline"));
        }

        [Test]
        public void TheLanguageChangedEventFiresOnlyWhenTheLanguageActuallyChanges()
        {
            var localisation = new Localiser();
            var changes = 0;
            localisation.LanguageChanged += () => changes++;

            localisation.Register(EnglishSample());
            Assert.AreEqual(1, changes, "selecting the first language is a change");

            localisation.Register(SpanishSample());
            Assert.AreEqual(1, changes, "registering a second language does not switch to it");

            localisation.SetLanguage("es");
            Assert.AreEqual(2, changes);

            localisation.SetLanguage("es");
            Assert.AreEqual(2, changes, "re-selecting the current language changes nothing");

            localisation.SetLanguage("fr");
            Assert.AreEqual(2, changes, "an unknown language changes nothing");
        }

        [Test]
        public void AMissingKeyFallsThroughToTheKeyRatherThanThrowing()
        {
            var localisation = TwoLanguages();

            Assert.AreEqual("report.disclaimer", localisation.Get("report.disclaimer"));
        }

        [Test]
        public void EveryMissingKeyIsRecordedOnceSoATranslatorCanBeHandedTheGaps()
        {
            var localisation = TwoLanguages();

            localisation.Get("report.disclaimer");
            localisation.Get("report.disclaimer");
            localisation.Get("headline");
            localisation.Get("objective.progress");

            CollectionAssert.AreEqual(
                new[] { "report.disclaimer", "objective.progress" },
                localisation.MissingKeys.ToList());
        }

        [Test]
        public void AKeyMissingOnlyFromTheSecondLanguageIsRecordedWhenItIsSelected()
        {
            var localisation = new Localiser();
            var english = EnglishSample();
            english.Set("disclaimer", "Educational demonstration only.");
            localisation.Register(english);
            localisation.Register(SpanishSample());

            Assert.AreEqual("Educational demonstration only.", localisation.Get("disclaimer"));
            Assert.AreEqual(0, localisation.MissingKeys.Count);

            localisation.SetLanguage("es");

            Assert.AreEqual("disclaimer", localisation.Get("disclaimer"));
            CollectionAssert.AreEqual(new[] { "disclaimer" }, localisation.MissingKeys.ToList());
        }

        [Test]
        public void RecordedGapsCanBeCleared()
        {
            var localisation = TwoLanguages();
            localisation.Get("missing.one");

            localisation.ClearMissingKeys();

            Assert.AreEqual(0, localisation.MissingKeys.Count);

            localisation.Get("missing.one");
            Assert.AreEqual(1, localisation.MissingKeys.Count);
        }

        [Test]
        public void PlaceholdersAreFilledIn()
        {
            var localisation = TwoLanguages();

            Assert.AreEqual("Score: 85", localisation.Get("score", 85));
            Assert.AreEqual("2/4", localisation.Get("progress", 2, 4));

            localisation.SetLanguage("es");
            Assert.AreEqual("Puntuación: 85", localisation.Get("score", 85));
        }

        [Test]
        public void PassingNoArgumentsLeavesTheStringUntouched()
        {
            var localisation = TwoLanguages();

            Assert.AreEqual("Score: {0}", localisation.Get("score"));
            Assert.AreEqual("Score: {0}", localisation.Get("score", new object[0]));
            Assert.AreEqual("Score: {0}", localisation.Get("score", null));
        }

        [Test]
        public void ArgumentsThatDoNotMatchThePlaceholdersReturnTheUnformattedString()
        {
            var localisation = TwoLanguages();

            // A translator who drops a placeholder must not be able to end a trainee's drill.
            Assert.AreEqual("{0}/{1}", localisation.Get("progress", 2));
            Assert.AreEqual("Score: 85", localisation.Get("score", 85, 4),
                "extra arguments are harmless and are simply ignored by string.Format");
        }

        [Test]
        public void FormattingAMissingKeyStillReturnsTheKey()
        {
            var localisation = TwoLanguages();

            Assert.AreEqual("report.summary.score", localisation.Get("report.summary.score", 85));
        }

        [Test]
        public void FiguresAreFormattedTheSameWayWhateverTheMachineLocale()
        {
            var localisation = TwoLanguages();
            var previous = CultureInfo.CurrentCulture;

            try
            {
                // A machine set to a comma-decimal locale must not change the reported figures,
                // which the rest of the reporting code writes with the invariant culture.
                var commaDecimal = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                commaDecimal.NumberFormat.NumberDecimalSeparator = ",";
                CultureInfo.CurrentCulture = commaDecimal;

                Assert.AreEqual("Score: 85.5", localisation.Get("score", 85.5));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
