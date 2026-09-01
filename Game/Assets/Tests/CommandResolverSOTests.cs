using System.Collections.Generic;
using Game.Core;
using Game.Features.Command;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class CommandResolverSOTests
    {
        private static readonly CommandEffect Train = new CommandEffect(-20, 8, -5, 20);
        private static readonly CommandEffect Rest = new CommandEffect(30, 0, 10, 0);

        private CommandResolverSO _resolver;
        private readonly List<Object> _createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _resolver = ScriptableObject.CreateInstance<CommandResolverSO>();
            _createdObjects.Add(_resolver);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        private GameRulesSO CreateRules(
            int paramMin, int paramMax,
            int initialStamina, int initialSkill, int initialMental,
            int startTurn, int maxTurn)
        {
            GameRulesSO rules = GameRulesSOFactory.Create(
                paramMin, paramMax, initialStamina, initialSkill, initialMental, startTurn, maxTurn);
            _createdObjects.Add(rules);
            return rules;
        }

        [Test]
        public void ChangesStaminaSkillMentalAccordingToTrainEffect()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 50 };

            CommandResult result = _resolver.Resolve(originalState, Train, rules);

            Assert.AreEqual(80, result.State.Stamina);
            Assert.AreEqual(8, result.State.Skill);
            Assert.AreEqual(45, result.State.Mental);
            Assert.IsTrue(result.IsExecutable);
            Assert.AreEqual(TerminationKind.Continue, result.Termination);
        }

        [Test]
        public void DoesNotMutateOriginalGameStateInstance()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 50 };

            CommandResult result = _resolver.Resolve(originalState, Train, rules);

            Assert.AreEqual(1, originalState.CurrentTurn);
            Assert.AreEqual(100, originalState.Stamina);
            Assert.AreEqual(0, originalState.Skill);
            Assert.AreEqual(50, originalState.Mental);
            Assert.IsFalse(ReferenceEquals(originalState, result.State));
        }

        [Test]
        public void ClampsSkillAtUpperBoundWhenDeltaExceedsRemainingCapacity()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 95, Mental = 50 };
            CommandEffect effect = new CommandEffect(0, 20, 0, 0);

            CommandResult result = _resolver.Resolve(originalState, effect, rules);

            Assert.AreEqual(100, result.State.Skill);
        }

        [Test]
        public void ClampsStaminaAtLowerBoundWhenDeltaExceedsRemainingStamina()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 10, Skill = 0, Mental = 50 };
            CommandEffect effect = new CommandEffect(-20, 0, 0, 0);

            CommandResult result = _resolver.Resolve(originalState, effect, rules);

            Assert.AreEqual(0, result.State.Stamina);
        }

        [Test]
        public void RejectsCommandAndLeavesStateUnchangedWhenStaminaInsufficient()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 10, Skill = 0, Mental = 50 };

            CommandResult result = _resolver.Resolve(originalState, Train, rules);

            Assert.IsFalse(result.IsExecutable);
            Assert.AreEqual(originalState, result.State);
            Assert.AreEqual(originalState.CurrentTurn, result.State.CurrentTurn);
        }

        [Test]
        public void AdvancesTurnByOneWhenCommandIsApplied()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 50 };

            CommandResult result = _resolver.Resolve(originalState, Train, rules);

            Assert.AreEqual(2, result.State.CurrentTurn);
            Assert.AreEqual(TerminationKind.Continue, result.Termination);
        }

        [Test]
        public void EndsNormallyWhenCommandAppliedOnFinalTurn()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 24, Stamina = 100, Skill = 0, Mental = 50 };

            CommandResult result = _resolver.Resolve(originalState, Train, rules);

            Assert.AreEqual(TerminationKind.NormalEnd, result.Termination);
            Assert.AreEqual(25, result.State.CurrentTurn);
        }

        [Test]
        public void EndsInGameOverAndDoesNotAdvanceTurnWhenMentalReachesZero()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState originalState = new GameState { CurrentTurn = 1, Stamina = 100, Skill = 0, Mental = 5 };
            CommandEffect effect = new CommandEffect(0, 0, -5, 0);

            CommandResult result = _resolver.Resolve(originalState, effect, rules);

            Assert.AreEqual(TerminationKind.GameOver, result.Termination);
            Assert.AreEqual(1, result.State.CurrentTurn);
            Assert.AreEqual(0, result.State.Mental);
        }

        [Test]
        public void PrioritizesGameOverWhenNormalEndWouldAlsoApply()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 5);
            GameState originalState = new GameState { CurrentTurn = 5, Stamina = 100, Skill = 0, Mental = 5 };
            CommandEffect effect = new CommandEffect(0, 0, -5, 0);

            CommandResult result = _resolver.Resolve(originalState, effect, rules);

            Assert.AreEqual(TerminationKind.GameOver, result.Termination);
            Assert.AreEqual(5, result.State.CurrentTurn);
        }

        [Test]
        public void EndsNormallyAtTurnTwentyFourAfterTwentyFourConsecutiveRests()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameState state = rules.CreateInitialState();

            CommandResult result = null;
            for (int i = 1; i <= 24; i++)
            {
                result = _resolver.Resolve(state, Rest, rules);

                Assert.IsTrue(result.IsExecutable);
                Assert.AreNotEqual(TerminationKind.GameOver, result.Termination);

                if (i < 24)
                {
                    Assert.AreEqual(TerminationKind.Continue, result.Termination);
                }
                else
                {
                    Assert.AreEqual(TerminationKind.NormalEnd, result.Termination);
                }

                state = result.State;
            }

            Assert.AreEqual(25, state.CurrentTurn);
        }
    }
}
