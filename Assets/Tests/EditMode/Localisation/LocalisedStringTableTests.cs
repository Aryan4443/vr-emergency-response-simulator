using System.Linq;
using NUnit.Framework;
using VRSim.Localisation;

namespace VRSim.Tests
{
    public class LocalisedStringTableTests
    {
        static LocalisedStringTable SampleTable()
        {
            var table = new LocalisedStringTable("en", "English");
            table.Set("greeting", "Hello");
            table.Set("farewell", "Goodbye");
            return table;
        }

        [Test]
        public void ATableRemembersItsLanguage()
        {
            var table = SampleTable();

            Assert.AreEqual("en", table.LanguageCode);
            Assert.AreEqual("English", table.DisplayName);
            Assert.IsTrue(table.IsValid);
        }

        [Test]
        public void AKnownKeyReturnsItsTranslation()
        {
            Assert.AreEqual("Hello", SampleTable().Get("greeting"));
        }

        [Test]
        public void AMissingKeyReturnsTheKeyItself()
        {
            var table = SampleTable();

            Assert.AreEqual("greeting.formal", table.Get("greeting.formal"));
            Assert.IsFalse(table.Has("greeting.formal"));
        }

        [Test]
        public void AMissingKeyNeverReturnsNull()
        {
            Assert.IsNotNull(SampleTable().Get("nothing.here"));
            Assert.AreEqual(string.Empty, SampleTable().Get(null));
            Assert.AreEqual(string.Empty, SampleTable().Get(string.Empty));
        }

        [Test]
        public void HasReportsWhetherTheKeyIsPresent()
        {
            var table = SampleTable();

            Assert.IsTrue(table.Has("greeting"));
            Assert.IsFalse(table.Has("missing"));
            Assert.IsFalse(table.Has(null));
        }

        [Test]
        public void SettingAKeyTwiceReplacesTheValueRatherThanDuplicatingIt()
        {
            var table = SampleTable();

            table.Set("greeting", "Hi");

            Assert.AreEqual("Hi", table.Get("greeting"));
            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(1, table.Keys.Count(k => k == "greeting"));
        }

        [Test]
        public void BlankKeysAreIgnored()
        {
            var table = SampleTable();

            table.Set(null, "value");
            table.Set(string.Empty, "value");
            table.Set("   ", "value");

            Assert.AreEqual(2, table.Count);
        }

        [Test]
        public void ANullValueIsStoredAsAnEmptyString()
        {
            var table = SampleTable();

            table.Set("blank", null);

            Assert.IsTrue(table.Has("blank"));
            Assert.AreEqual(string.Empty, table.Get("blank"));
        }

        [Test]
        public void KeysAreListedInInsertionOrderSoTwoLanguagesCanBeDiffed()
        {
            CollectionAssert.AreEqual(new[] { "greeting", "farewell" }, SampleTable().Keys.ToList());
        }

        [Test]
        public void JsonRoundTripPreservesTheLanguageAndEveryEntry()
        {
            var original = SampleTable();
            original.Set("punctuation", "Stay out of smoke — it is unsafe.");

            var restored = LocalisedStringTable.FromJson(original.ToJson());

            Assert.AreEqual("en", restored.LanguageCode);
            Assert.AreEqual("English", restored.DisplayName);
            Assert.AreEqual(original.Count, restored.Count);
            Assert.AreEqual("Hello", restored.Get("greeting"));
            Assert.AreEqual("Goodbye", restored.Get("farewell"));
            Assert.AreEqual("Stay out of smoke — it is unsafe.", restored.Get("punctuation"));
        }

        [Test]
        public void ATableStaysEditableAfterBeingLoaded()
        {
            var restored = LocalisedStringTable.FromJson(SampleTable().ToJson());

            restored.Set("greeting", "Hi");
            restored.Set("added", "Added");

            Assert.AreEqual("Hi", restored.Get("greeting"));
            Assert.AreEqual("Added", restored.Get("added"));
            Assert.AreEqual(3, restored.Count);
        }

        [Test]
        public void CorruptJsonFallsBackToAnEmptyTable()
        {
            var table = LocalisedStringTable.FromJson("{ this is not json");

            Assert.AreEqual(0, table.Count);
            Assert.IsFalse(table.IsValid, "a fallback table must not pretend to be a real language");
            Assert.AreEqual("greeting", table.Get("greeting"));
        }

        [Test]
        public void MissingJsonFallsBackToAnEmptyTable()
        {
            Assert.AreEqual(0, LocalisedStringTable.FromJson(null).Count);
            Assert.AreEqual(0, LocalisedStringTable.FromJson(string.Empty).Count);
            Assert.AreEqual(0, LocalisedStringTable.FromJson("   ").Count);
        }

        [Test]
        public void ADuplicatedKeyInAFileTakesItsLastValueRatherThanThrowing()
        {
            const string json =
                "{\"m_LanguageCode\":\"en\",\"m_DisplayName\":\"English\"," +
                "\"m_Keys\":[\"greeting\",\"greeting\"],\"m_Values\":[\"Hello\",\"Hi\"]}";

            var table = LocalisedStringTable.FromJson(json);

            Assert.AreEqual("Hi", table.Get("greeting"));
        }

        [Test]
        public void UnevenKeyAndValueListsDegradeToTheEntriesThatPairUp()
        {
            const string json =
                "{\"m_LanguageCode\":\"en\",\"m_DisplayName\":\"English\"," +
                "\"m_Keys\":[\"greeting\",\"farewell\"],\"m_Values\":[\"Hello\"]}";

            var table = LocalisedStringTable.FromJson(json);

            Assert.AreEqual("Hello", table.Get("greeting"));
            Assert.AreEqual("farewell", table.Get("farewell"));
        }

        [Test]
        public void AFileWithNoEntriesAtAllStillProducesAUsableTable()
        {
            var table = LocalisedStringTable.FromJson("{\"m_LanguageCode\":\"es\",\"m_DisplayName\":\"Espanol\"}");

            Assert.AreEqual("es", table.LanguageCode);
            Assert.AreEqual(0, table.Count);
            Assert.AreEqual("anything", table.Get("anything"));
        }
    }
}
