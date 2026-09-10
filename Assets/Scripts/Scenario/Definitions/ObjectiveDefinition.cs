using System;
using UnityEngine;

namespace VRSim.Scenario.Definitions
{
    /// <summary>
    /// The authored description of one objective.
    ///
    /// Kept separate from the runtime <see cref="Objective"/> because a definition is shared by
    /// every run of a scenario, while completion state belongs to a single run. Serialised through
    /// backing fields rather than the properties, since JsonUtility only ever sees fields.
    /// </summary>
    [Serializable]
    public class ObjectiveDefinition
    {
        [SerializeField] string m_Id;
        [SerializeField] string m_Title;
        [SerializeField] string m_Hint;

        /// <summary>Needed by JsonUtility and by the instructor mode described in section 20.</summary>
        public ObjectiveDefinition()
        {
        }

        /// <param name="id">Stable identifier the scenario uses to mark the step done.</param>
        /// <param name="title">Short instruction shown in the objective list.</param>
        /// <param name="hint">Where to go or what to look for while this step is current.</param>
        public ObjectiveDefinition(string id, string title, string hint)
        {
            m_Id = id;
            m_Title = title;
            m_Hint = hint;
        }

        /// <summary>
        /// Stable identifier. Scenario code refers to this string, so it has to survive a rename
        /// of the title and stay unique inside its scenario.
        /// </summary>
        public string Id
        {
            get => m_Id;
            set => m_Id = value;
        }

        /// <summary>Short instruction shown in the objective list.</summary>
        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <summary>Where to go or what to look for, shown while this step is current.</summary>
        public string Hint
        {
            get => m_Hint;
            set => m_Hint = value;
        }

        /// <summary>Builds the runtime objective this definition describes.</summary>
        public Objective ToObjective() => new Objective(m_Id, m_Title, m_Hint);
    }
}
