using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace VRSim.Evaluation
{
    /// <summary>
    /// Turns a <see cref="ScenarioResult"/> into the text shown on the results screen described in
    /// section 4.6: completion time, mistakes, unsafe-area entries, selected exit, score and
    /// improvement guidance.
    ///
    /// Kept separate from the UI components so the wording and the guidance rules can be unit
    /// tested without building a canvas.
    /// </summary>
    public static class ScenarioReport
    {
        /// <summary>
        /// Required by sections 2 and 10: the prototype must state plainly that it is an
        /// educational demonstration rather than a certified training product.
        /// </summary>
        public const string Disclaimer =
            "This is an educational demonstration. It is not certified emergency training and " +
            "does not qualify anyone to respond to a real emergency.";

        /// <summary>Formats a duration as minutes and seconds, for example 1:32.</summary>
        public static string FormatTime(float seconds)
        {
            var clamped = Mathf.Max(0f, seconds);
            var whole = Mathf.FloorToInt(clamped);
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", whole / 60, whole % 60);
        }

        /// <summary>One-line verdict for the top of the results screen.</summary>
        public static string Headline(ScenarioResult result) =>
            result.completed ? "Evacuated safely" : "Did not reach a safe exit";

        /// <summary>The figures from section 4.6, one per line.</summary>
        public static IEnumerable<string> SummaryLines(ScenarioResult result)
        {
            yield return $"Time: {FormatTime(result.completionTimeSeconds)}";
            yield return $"Unsafe areas entered: {result.unsafeZoneEntries}";
            yield return $"Incorrect actions: {result.incorrectActions}";
            yield return $"Exit taken: {(string.IsNullOrEmpty(result.selectedExit) ? "—" : result.selectedExit)}";
            yield return $"Score: {result.score}";
        }

        /// <summary>
        /// Improvement guidance. Each rule points at one thing the user can do differently next
        /// run, phrased as advice rather than as a judgement.
        /// </summary>
        public static IEnumerable<string> Guidance(ScenarioResult result)
        {
            var advice = new List<string>();

            if (!result.completed)
                advice.Add("Follow the green exit signs. They always point to the safe route.");

            if (result.unsafeZoneEntries > 0)
                advice.Add("Stay out of smoke. Smoke-filled areas are unsafe even when they look " +
                           "like the shortest way out.");

            if (!DidAction(result, "map_viewed"))
                advice.Add("Read the evacuation map before you move. Knowing the route first saves " +
                           "time later.");

            if (!DidAction(result, "alarm_activated"))
                advice.Add("Activate the fire alarm on your way out so other people are warned.");

            if (result.incorrectActions > 0)
                advice.Add("Check where a door leads before committing to it. Blocked routes cost " +
                           "time you may not have.");

            if (advice.Count == 0)
                advice.Add("A clean run: you read the map, raised the alarm and took the safe exit. " +
                           "Try repeating it faster.");

            return advice;
        }

        static bool DidAction(ScenarioResult result, string actionType) =>
            result.events != null && result.events.Any(e => e.actionType == actionType);
    }
}
