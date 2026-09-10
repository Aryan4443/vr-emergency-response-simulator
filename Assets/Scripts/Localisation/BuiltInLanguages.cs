using System.Collections.Generic;

namespace VRSim.Localisation
{
    /// <summary>
    /// The languages that ship with the prototype: English, whose wording is exactly what the
    /// simulation shows today, and Spanish, a genuine translation of the same set of keys.
    ///
    /// Built in code rather than loaded from a file so the default languages cannot go missing on
    /// a device, and so a test can prove the two tables cover the same keys. Additional languages
    /// are expected to arrive as JSON and be registered through
    /// <see cref="Localiser.Register"/>.
    /// </summary>
    public static class BuiltInLanguages
    {
        /// <summary>Code of the built-in English table, and the sensible default language.</summary>
        public const string EnglishCode = "en";

        /// <summary>Code of the built-in Spanish table.</summary>
        public const string SpanishCode = "es";

        /// <summary>
        /// English, using the exact wording already in <see cref="VRSim.Evaluation.ScenarioReport"/>
        /// and <see cref="VRSim.Scenario.ObjectiveTracker"/>, so routing the UI through the table
        /// changes nothing visually.
        /// </summary>
        public static LocalisedStringTable English()
        {
            var table = new LocalisedStringTable(EnglishCode, "English");

            table.Set(Localiser.Keys.ReportDisclaimer,
                "This is an educational demonstration. It is not certified emergency training and " +
                "does not qualify anyone to respond to a real emergency.");

            table.Set(Localiser.Keys.ReportHeadlineSuccess, "Evacuated safely");
            table.Set(Localiser.Keys.ReportHeadlineFailure, "Did not reach a safe exit");

            table.Set(Localiser.Keys.ReportSummaryTime, "Time: {0}");
            table.Set(Localiser.Keys.ReportSummaryUnsafeAreas, "Unsafe areas entered: {0}");
            table.Set(Localiser.Keys.ReportSummaryIncorrectActions, "Incorrect actions: {0}");
            table.Set(Localiser.Keys.ReportSummaryExitTaken, "Exit taken: {0}");
            table.Set(Localiser.Keys.ReportSummaryScore, "Score: {0}");
            table.Set(Localiser.Keys.ReportSummaryNoExit, "—");

            table.Set(Localiser.Keys.GuidanceFollowExitSigns,
                "Follow the green exit signs. They always point to the safe route.");
            table.Set(Localiser.Keys.GuidanceAvoidSmoke,
                "Stay out of smoke. Smoke-filled areas are unsafe even when they look " +
                "like the shortest way out.");
            table.Set(Localiser.Keys.GuidanceReadMap,
                "Read the evacuation map before you move. Knowing the route first saves " +
                "time later.");
            table.Set(Localiser.Keys.GuidanceRaiseAlarm,
                "Activate the fire alarm on your way out so other people are warned.");
            table.Set(Localiser.Keys.GuidanceCheckDoors,
                "Check where a door leads before committing to it. Blocked routes cost " +
                "time you may not have.");
            table.Set(Localiser.Keys.GuidanceCleanRun,
                "A clean run: you read the map, raised the alarm and took the safe exit. " +
                "Try repeating it faster.");

            table.Set(Localiser.Keys.ObjectiveReadMapTitle, "Read the evacuation map");
            table.Set(Localiser.Keys.ObjectiveReadMapHint,
                "The white board on the right-hand wall of the hallway.");
            table.Set(Localiser.Keys.ObjectiveRaiseAlarmTitle, "Raise the fire alarm");
            table.Set(Localiser.Keys.ObjectiveRaiseAlarmHint,
                "The red box on the left-hand wall, opposite the map.");
            table.Set(Localiser.Keys.ObjectiveAvoidHazardTitle, "Keep out of the smoke");
            table.Set(Localiser.Keys.ObjectiveAvoidHazardHint,
                "The south end of the hallway is filling with smoke. Do not walk into it.");
            table.Set(Localiser.Keys.ObjectiveReachExitTitle, "Leave by the safe exit");
            table.Set(Localiser.Keys.ObjectiveReachExitHint,
                "Follow the green exit sign at the north end of the hallway.");

            table.Set(Localiser.Keys.ObjectiveProgress, "{0}/{1}");

            return table;
        }

        /// <summary>
        /// Spanish. The placeholders are kept exactly as the English lines use them, because the
        /// figures they carry are produced by the same reporting code whatever the language.
        /// </summary>
        public static LocalisedStringTable Spanish()
        {
            var table = new LocalisedStringTable(SpanishCode, "Espanol");

            table.Set(Localiser.Keys.ReportDisclaimer,
                "Esta es una demostración educativa. No es formación certificada en emergencias y " +
                "no capacita a nadie para responder a una emergencia real.");

            table.Set(Localiser.Keys.ReportHeadlineSuccess, "Evacuación segura");
            table.Set(Localiser.Keys.ReportHeadlineFailure, "No llegó a una salida segura");

            table.Set(Localiser.Keys.ReportSummaryTime, "Tiempo: {0}");
            table.Set(Localiser.Keys.ReportSummaryUnsafeAreas, "Entradas en zonas inseguras: {0}");
            table.Set(Localiser.Keys.ReportSummaryIncorrectActions, "Acciones incorrectas: {0}");
            table.Set(Localiser.Keys.ReportSummaryExitTaken, "Salida utilizada: {0}");
            table.Set(Localiser.Keys.ReportSummaryScore, "Puntuación: {0}");
            table.Set(Localiser.Keys.ReportSummaryNoExit, "—");

            table.Set(Localiser.Keys.GuidanceFollowExitSigns,
                "Siga las señales verdes de salida. Siempre indican la ruta segura.");
            table.Set(Localiser.Keys.GuidanceAvoidSmoke,
                "Manténgase fuera del humo. Las zonas llenas de humo son peligrosas aunque " +
                "parezcan el camino más corto hacia la salida.");
            table.Set(Localiser.Keys.GuidanceReadMap,
                "Lea el plano de evacuación antes de moverse. Conocer la ruta de antemano " +
                "ahorra tiempo después.");
            table.Set(Localiser.Keys.GuidanceRaiseAlarm,
                "Active la alarma de incendios al salir para avisar a las demás personas.");
            table.Set(Localiser.Keys.GuidanceCheckDoors,
                "Compruebe adónde lleva una puerta antes de cruzarla. Las rutas bloqueadas " +
                "cuestan un tiempo del que quizá no disponga.");
            table.Set(Localiser.Keys.GuidanceCleanRun,
                "Un recorrido impecable: leyó el plano, dio la alarma y tomó la salida segura. " +
                "Intente repetirlo más rápido.");

            table.Set(Localiser.Keys.ObjectiveReadMapTitle, "Lea el plano de evacuación");
            table.Set(Localiser.Keys.ObjectiveReadMapHint,
                "El panel blanco de la pared derecha del pasillo.");
            table.Set(Localiser.Keys.ObjectiveRaiseAlarmTitle, "Active la alarma de incendios");
            table.Set(Localiser.Keys.ObjectiveRaiseAlarmHint,
                "La caja roja de la pared izquierda, frente al plano.");
            table.Set(Localiser.Keys.ObjectiveAvoidHazardTitle, "Manténgase fuera del humo");
            table.Set(Localiser.Keys.ObjectiveAvoidHazardHint,
                "El extremo sur del pasillo se está llenando de humo. No entre en él.");
            table.Set(Localiser.Keys.ObjectiveReachExitTitle, "Salga por la salida segura");
            table.Set(Localiser.Keys.ObjectiveReachExitHint,
                "Siga la señal verde de salida del extremo norte del pasillo.");

            table.Set(Localiser.Keys.ObjectiveProgress, "{0}/{1}");

            return table;
        }

        /// <summary>Every built-in table, in menu order.</summary>
        public static IReadOnlyList<LocalisedStringTable> All() => new[]
        {
            English(),
            Spanish(),
        };

        /// <summary>
        /// A ready-to-use lookup with both built-in languages registered and English selected,
        /// which is what the UI should ask for at start-up.
        /// </summary>
        public static Localiser CreateDefault()
        {
            var localisation = new Localiser();
            foreach (var table in All())
                localisation.Register(table);

            localisation.SetLanguage(EnglishCode);
            return localisation;
        }
    }
}
