using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VRSim.Localisation
{
    /// <summary>
    /// The lookup every piece of UI goes through to turn a key into words.
    ///
    /// One instance owns the registered languages and the current selection, so the UI asks
    /// <c>Get(Keys.ReportDisclaimer)</c> and never has to know which language is active. Plain C#
    /// with no Unity dependency, so the wording rules are unit tested directly.
    /// </summary>
    public class Localiser
    {
        // A handful of languages at most, so a list kept in registration order is clearer than a
        // dictionary and gives callers a stable order for the language menu.
        readonly List<LocalisedStringTable> m_Tables = new List<LocalisedStringTable>();

        readonly List<string> m_MissingKeys = new List<string>();
        readonly HashSet<string> m_MissingKeySet = new HashSet<string>(StringComparer.Ordinal);

        LocalisedStringTable m_Current;

        /// <summary>Raised whenever the selected language actually changes, so the UI can redraw.</summary>
        public event Action LanguageChanged;

        /// <summary>Code of the selected language, or an empty string before one is registered.</summary>
        public string CurrentLanguage => m_Current != null ? m_Current.LanguageCode : string.Empty;

        /// <summary>Name of the selected language in its own language, for the options menu.</summary>
        public string CurrentDisplayName => m_Current != null ? m_Current.DisplayName : string.Empty;

        /// <summary>Codes of every registered language, in the order they were registered.</summary>
        public IReadOnlyList<string> AvailableLanguages =>
            m_Tables.Select(t => t.LanguageCode).ToList();

        /// <summary>
        /// Every key that was asked for and could not be translated, in the order it was first
        /// missed, without duplicates.
        ///
        /// Collecting is deliberate. Throwing on a missing key would end a trainee's drill over a
        /// single untranslated label, which is exactly the wrong trade in a safety training app:
        /// the run matters more than the wording. Recording the gap instead means the list can be
        /// dumped after a play-test and handed to a translator as the precise set of lines still
        /// owed, while the trainee sees the key and carries on.
        /// </summary>
        public IReadOnlyList<string> MissingKeys => m_MissingKeys;

        /// <summary>
        /// Adds a language, replacing any table already registered under the same code. The first
        /// valid table registered also becomes the selected language, so a caller that registers
        /// English first is immediately usable.
        /// </summary>
        /// <returns>False when the table is null or has no language code, such as one recovered
        /// from a corrupt file; such a table is not registered.</returns>
        public bool Register(LocalisedStringTable table)
        {
            if (table == null || !table.IsValid)
                return false;

            var existing = IndexOf(table.LanguageCode);
            if (existing >= 0)
            {
                var wasCurrent = ReferenceEquals(m_Tables[existing], m_Current);
                m_Tables[existing] = table;
                if (wasCurrent)
                    m_Current = table;
            }
            else
                m_Tables.Add(table);

            if (m_Current == null)
            {
                m_Current = table;
                LanguageChanged?.Invoke();
            }

            return true;
        }

        /// <summary>True when a language with this code has been registered.</summary>
        public bool HasLanguage(string languageCode) => IndexOf(languageCode) >= 0;

        /// <summary>
        /// Selects a language by code, comparing case-insensitively so "EN" and "en" both work.
        /// </summary>
        /// <returns>False when no such language is registered, in which case the current language
        /// is left alone. Callers can then keep showing the language the user already had rather
        /// than dropping them into an empty UI.</returns>
        public bool SetLanguage(string languageCode)
        {
            var index = IndexOf(languageCode);
            if (index < 0)
                return false;

            var table = m_Tables[index];
            if (ReferenceEquals(table, m_Current))
                return true;

            m_Current = table;
            LanguageChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// The string for this key in the current language, or the key itself when it is missing or
        /// no language has been registered. Never throws and never returns null.
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (m_Current != null && m_Current.Has(key))
                return m_Current.Get(key);

            RecordMissing(key);
            return key;
        }

        /// <summary>
        /// The string for this key with <c>{0}</c>-style placeholders filled in, for lines such as
        /// "Score: {0}".
        ///
        /// Formatting is done with the invariant culture so the figures read the same way the rest
        /// of the reporting code writes them. If the arguments do not match the placeholders, which
        /// is easy for a translator to cause by dropping a "{0}", the unformatted string is returned
        /// rather than an exception: a slightly wrong label is recoverable mid-drill, a crash is not.
        /// </summary>
        public string Get(string key, params object[] args)
        {
            var text = Get(key);
            if (args == null || args.Length == 0)
                return text;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, args);
            }
            catch (FormatException)
            {
                return text;
            }
            catch (ArgumentNullException)
            {
                return text;
            }
        }

        /// <summary>Forgets the recorded gaps, for example at the start of a fresh play-test.</summary>
        public void ClearMissingKeys()
        {
            m_MissingKeys.Clear();
            m_MissingKeySet.Clear();
        }

        void RecordMissing(string key)
        {
            if (m_MissingKeySet.Add(key))
                m_MissingKeys.Add(key);
        }

        int IndexOf(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return -1;

            for (var i = 0; i < m_Tables.Count; i++)
            {
                if (string.Equals(m_Tables[i].LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// The key for every string the simulation currently shows, taken from
        /// <see cref="VRSim.Evaluation.ScenarioReport"/> and
        /// <see cref="VRSim.Scenario.ObjectiveTracker"/>.
        ///
        /// Constants rather than bare literals: a mistyped key would otherwise fall through to
        /// itself and show a raw identifier on the results screen, and the compiler is the cheapest
        /// place to catch that. <see cref="All"/> lets a tool list what a new language still owes.
        /// </summary>
        public static class Keys
        {
            /// <summary>The educational-demonstration disclaimer required by sections 2 and 10.</summary>
            public const string ReportDisclaimer = "report.disclaimer";

            /// <summary>Results headline when the user reached a safe exit.</summary>
            public const string ReportHeadlineSuccess = "report.headline.success";

            /// <summary>Results headline when the user did not reach a safe exit.</summary>
            public const string ReportHeadlineFailure = "report.headline.failure";

            /// <summary>Completion time line. Takes the formatted time, for example "1:32".</summary>
            public const string ReportSummaryTime = "report.summary.time";

            /// <summary>Unsafe-area entry count line. Takes the count.</summary>
            public const string ReportSummaryUnsafeAreas = "report.summary.unsafeAreas";

            /// <summary>Incorrect action count line. Takes the count.</summary>
            public const string ReportSummaryIncorrectActions = "report.summary.incorrectActions";

            /// <summary>Selected exit line. Takes the exit name.</summary>
            public const string ReportSummaryExitTaken = "report.summary.exitTaken";

            /// <summary>Score line. Takes the score.</summary>
            public const string ReportSummaryScore = "report.summary.score";

            /// <summary>Stand-in shown for the exit when the user never reached one.</summary>
            public const string ReportSummaryNoExit = "report.summary.noExit";

            /// <summary>Guidance given when the user did not evacuate.</summary>
            public const string GuidanceFollowExitSigns = "report.guidance.followExitSigns";

            /// <summary>Guidance given when the user walked into smoke.</summary>
            public const string GuidanceAvoidSmoke = "report.guidance.avoidSmoke";

            /// <summary>Guidance given when the user never read the evacuation map.</summary>
            public const string GuidanceReadMap = "report.guidance.readMap";

            /// <summary>Guidance given when the user never raised the alarm.</summary>
            public const string GuidanceRaiseAlarm = "report.guidance.raiseAlarm";

            /// <summary>Guidance given when the user took blocked or wrong routes.</summary>
            public const string GuidanceCheckDoors = "report.guidance.checkDoors";

            /// <summary>Guidance given when there is nothing to correct.</summary>
            public const string GuidanceCleanRun = "report.guidance.cleanRun";

            /// <summary>Title of the fire-drill step "read_map".</summary>
            public const string ObjectiveReadMapTitle = "objective.readMap.title";

            /// <summary>Hint for the fire-drill step "read_map".</summary>
            public const string ObjectiveReadMapHint = "objective.readMap.hint";

            /// <summary>Title of the fire-drill step "raise_alarm".</summary>
            public const string ObjectiveRaiseAlarmTitle = "objective.raiseAlarm.title";

            /// <summary>Hint for the fire-drill step "raise_alarm".</summary>
            public const string ObjectiveRaiseAlarmHint = "objective.raiseAlarm.hint";

            /// <summary>Title of the fire-drill step "avoid_hazard".</summary>
            public const string ObjectiveAvoidHazardTitle = "objective.avoidHazard.title";

            /// <summary>Hint for the fire-drill step "avoid_hazard".</summary>
            public const string ObjectiveAvoidHazardHint = "objective.avoidHazard.hint";

            /// <summary>Title of the fire-drill step "reach_exit".</summary>
            public const string ObjectiveReachExitTitle = "objective.reachExit.title";

            /// <summary>Hint for the fire-drill step "reach_exit".</summary>
            public const string ObjectiveReachExitHint = "objective.reachExit.hint";

            /// <summary>Objective progress readout. Takes completed count and total, as in "2/4".</summary>
            public const string ObjectiveProgress = "objective.progress";

            /// <summary>
            /// Every key above, so a translation can be checked for completeness without reflection.
            /// Kept in sync by a test that reflects over the constants in this class.
            /// </summary>
            public static readonly IReadOnlyList<string> All = new[]
            {
                ReportDisclaimer,
                ReportHeadlineSuccess,
                ReportHeadlineFailure,
                ReportSummaryTime,
                ReportSummaryUnsafeAreas,
                ReportSummaryIncorrectActions,
                ReportSummaryExitTaken,
                ReportSummaryScore,
                ReportSummaryNoExit,
                GuidanceFollowExitSigns,
                GuidanceAvoidSmoke,
                GuidanceReadMap,
                GuidanceRaiseAlarm,
                GuidanceCheckDoors,
                GuidanceCleanRun,
                ObjectiveReadMapTitle,
                ObjectiveReadMapHint,
                ObjectiveRaiseAlarmTitle,
                ObjectiveRaiseAlarmHint,
                ObjectiveAvoidHazardTitle,
                ObjectiveAvoidHazardHint,
                ObjectiveReachExitTitle,
                ObjectiveReachExitHint,
                ObjectiveProgress,
            };
        }
    }
}
