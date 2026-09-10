using NUnit.Framework;
using VRSim.Scenario;

namespace VRSim.Tests
{
    public class ScenarioStateMachineTests
    {
        [Test]
        public void StartsInReady()
        {
            var machine = new ScenarioStateMachine();

            Assert.AreEqual(ScenarioState.Ready, machine.CurrentState);
        }

        [Test]
        public void MovesFromReadyToBriefing()
        {
            var machine = new ScenarioStateMachine();

            var moved = machine.TryTransitionTo(ScenarioState.Briefing);

            Assert.IsTrue(moved);
            Assert.AreEqual(ScenarioState.Briefing, machine.CurrentState);
        }

        [Test]
        public void RejectsCompletingBeforeTheScenarioStarts()
        {
            var machine = new ScenarioStateMachine();

            var moved = machine.TryTransitionTo(ScenarioState.Completed);

            Assert.IsFalse(moved);
            Assert.AreEqual(ScenarioState.Ready, machine.CurrentState);
        }

        /// <summary>Walks a fresh machine to <paramref name="target"/> using legal transitions only.</summary>
        static ScenarioStateMachine MachineIn(ScenarioState target)
        {
            var machine = new ScenarioStateMachine();
            if (target == ScenarioState.Ready)
                return machine;

            machine.TryTransitionTo(ScenarioState.Briefing);
            if (target == ScenarioState.Briefing)
                return machine;

            machine.TryTransitionTo(ScenarioState.Active);
            switch (target)
            {
                case ScenarioState.Active:
                    return machine;
                case ScenarioState.Warning:
                    machine.TryTransitionTo(ScenarioState.Warning);
                    return machine;
                case ScenarioState.Failed:
                    machine.TryTransitionTo(ScenarioState.Failed);
                    return machine;
                default:
                    machine.TryTransitionTo(ScenarioState.Completed);
                    if (target == ScenarioState.Completed)
                        return machine;

                    machine.TryTransitionTo(ScenarioState.Review);
                    return machine;
            }
        }

        [Test]
        public void MachineInReachesTheRequestedState()
        {
            foreach (ScenarioState state in System.Enum.GetValues(typeof(ScenarioState)))
            {
                if (state == ScenarioState.Reset)
                    continue;

                Assert.AreEqual(state, MachineIn(state).CurrentState);
            }
        }

        [Test]
        public void CanBeResetFromAnyState()
        {
            // Section 13 requires a pause menu with an immediate exit, so abandoning a run has to
            // work from wherever the user happens to be, not only from the results screen.
            foreach (ScenarioState state in System.Enum.GetValues(typeof(ScenarioState)))
            {
                if (state == ScenarioState.Reset || state == ScenarioState.Ready)
                    continue;

                var machine = MachineIn(state);

                Assert.IsTrue(machine.TryTransitionTo(ScenarioState.Reset),
                    $"could not reset from {state}");
                Assert.IsTrue(machine.TryTransitionTo(ScenarioState.Ready),
                    $"could not return to Ready after resetting from {state}");
            }
        }

        [Test]
        public void RaisesStateChangedWithPreviousAndNextState()
        {
            var machine = new ScenarioStateMachine();
            ScenarioState? from = null;
            ScenarioState? to = null;
            machine.StateChanged += (previous, next) => { from = previous; to = next; };

            machine.TryTransitionTo(ScenarioState.Briefing);

            Assert.AreEqual(ScenarioState.Ready, from);
            Assert.AreEqual(ScenarioState.Briefing, to);
        }

        [Test]
        public void DoesNotRaiseStateChangedForARejectedTransition()
        {
            var machine = new ScenarioStateMachine();
            var raised = false;
            machine.StateChanged += (previous, next) => raised = true;

            machine.TryTransitionTo(ScenarioState.Completed);

            Assert.IsFalse(raised);
        }
    }
}
