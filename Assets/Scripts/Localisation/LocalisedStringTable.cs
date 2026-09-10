using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRSim.Localisation
{
    /// <summary>
    /// Every displayable string for one language, keyed by the stable identifiers in
    /// <see cref="Localiser.Keys"/>.
    ///
    /// The specification lists multilingual instructions and subtitles as future work, and
    /// accessibility is a core theme: someone taking safety training in their second language is
    /// precisely the person a translated table helps. Keeping the table as plain C# with no Unity
    /// dependency beyond JSON means the wording can be unit tested without building a canvas, in
    /// the same way <see cref="VRSim.Evaluation.ScenarioReport"/> is.
    ///
    /// A missing translation is never an error here. <see cref="Get"/> hands back the key itself,
    /// which is always displayable and makes the gap obvious the moment anyone looks at the screen.
    /// </summary>
    [Serializable]
    public class LocalisedStringTable
    {
        // JsonUtility cannot serialise a Dictionary: it only understands fields whose types Unity's
        // own serialiser handles, and Dictionary is not one of them (neither are interfaces,
        // properties, or nested generic containers such as List<List<string>>). The standard
        // workaround, used here, is to store two parallel Lists that Unity can serialise and to
        // rebuild the lookup Dictionary after loading. The lists are the persisted truth; the
        // dictionary is a derived index that is thrown away and rebuilt whenever the lists change.
        [SerializeField] string m_LanguageCode = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] List<string> m_Keys = new List<string>();
        [SerializeField] List<string> m_Values = new List<string>();

        // Not serialised, and deliberately rebuilt lazily: JsonUtility writes straight into the
        // fields above without going through any of our code, so the only safe moment to build the
        // index is the first read after a change.
        [NonSerialized] Dictionary<string, string> m_Lookup;

        /// <summary>
        /// Creates an empty table. Required by <see cref="JsonUtility"/>, which constructs the
        /// instance before writing the deserialised fields into it.
        /// </summary>
        public LocalisedStringTable()
        {
        }

        /// <summary>Creates an empty table for one language.</summary>
        /// <param name="languageCode">Short code such as "en" or "es". Compared case-insensitively.</param>
        /// <param name="displayName">The language's name in its own language, for the options menu.</param>
        public LocalisedStringTable(string languageCode, string displayName)
        {
            m_LanguageCode = languageCode ?? string.Empty;
            m_DisplayName = displayName ?? string.Empty;
        }

        /// <summary>Short code such as "en" or "es", used to select the language at runtime.</summary>
        public string LanguageCode => m_LanguageCode ?? string.Empty;

        /// <summary>The language's name as a speaker of it would read it, for the options menu.</summary>
        public string DisplayName => m_DisplayName ?? string.Empty;

        /// <summary>
        /// The keys this table carries, in insertion order so two tables can be diffed line by line
        /// when a translation is being checked.
        /// </summary>
        public IReadOnlyList<string> Keys
        {
            get
            {
                EnsureLists();
                return m_Keys;
            }
        }

        /// <summary>How many strings the table holds.</summary>
        public int Count
        {
            get
            {
                EnsureLists();
                return m_Keys.Count;
            }
        }

        /// <summary>
        /// True once the table has a language code, which is all that is needed to register it.
        /// A table recovered from a corrupt file fails this, so it can be rejected rather than
        /// silently shadowing a real language.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(m_LanguageCode);

        /// <summary>
        /// Adds or replaces one string. Null or blank keys are ignored, and a null value is stored
        /// as an empty string, so no caller can put an unusable entry into the table.
        /// </summary>
        public void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            EnsureLists();

            var existing = m_Keys.IndexOf(key);
            if (existing >= 0)
                m_Values[existing] = value ?? string.Empty;
            else
            {
                m_Keys.Add(key);
                m_Values.Add(value ?? string.Empty);
            }

            m_Lookup = null;
        }

        /// <summary>True when this table can translate the key.</summary>
        public bool Has(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return Lookup().ContainsKey(key);
        }

        /// <summary>
        /// The translated string, or the key itself when there is no translation.
        ///
        /// This never throws and never returns null. A half-finished translation must still leave
        /// the trainee with something readable on screen, and echoing the key back makes the gap
        /// obvious during testing instead of hiding it behind a blank label.
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return Lookup().TryGetValue(key, out var value) && value != null ? value : key;
        }

        /// <summary>Serialises the table, matching the JSON pattern used for the accessibility settings.</summary>
        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>
        /// Restores a table, falling back to an empty one when the text is unusable. A corrupt
        /// language file must never stop someone starting the simulation; the fallback table simply
        /// echoes keys back, and <see cref="IsValid"/> is false so it can be rejected on registration.
        /// </summary>
        public static LocalisedStringTable FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new LocalisedStringTable();

            try
            {
                var table = JsonUtility.FromJson<LocalisedStringTable>(json) ?? new LocalisedStringTable();
                table.EnsureLists();
                table.m_Lookup = null;
                return table;
            }
            catch (Exception)
            {
                return new LocalisedStringTable();
            }
        }

        /// <summary>
        /// Rebuilds the dictionary from the parallel lists. Tolerates the damage a hand-edited or
        /// truncated file can do: unequal list lengths, blank keys and duplicated keys.
        /// </summary>
        Dictionary<string, string> Lookup()
        {
            if (m_Lookup != null)
                return m_Lookup;

            EnsureLists();

            var lookup = new Dictionary<string, string>(m_Keys.Count, StringComparer.Ordinal);
            var pairs = Math.Min(m_Keys.Count, m_Values.Count);
            for (var i = 0; i < pairs; i++)
            {
                var key = m_Keys[i];
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                // Indexer rather than Add: a duplicated key in a file is a translator's mistake,
                // not a reason to refuse to show the language at all. The last entry wins.
                lookup[key] = m_Values[i] ?? string.Empty;
            }

            m_Lookup = lookup;
            return m_Lookup;
        }

        void EnsureLists()
        {
            // JsonUtility can leave a list field null when the JSON omits it or spells it null.
            m_Keys ??= new List<string>();
            m_Values ??= new List<string>();
        }
    }
}
