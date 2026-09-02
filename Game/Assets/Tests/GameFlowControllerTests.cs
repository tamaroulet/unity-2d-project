// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GameFlowControllerTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object createdObject in _createdObjects)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void StartGame_TransitionsToWaitingInputAndRaisesInitialStateWhenNoEventFires()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);

            controller.StartGame();

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
            Assert.AreEqual(1, raisedStates.Count);
            Assert.AreEqual(100, controller.CurrentState.Stamina);
            Assert.AreEqual(0, controller.CurrentState.Skill);
            Assert.AreEqual(50, controller.CurrentState.Mental);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
        }

        [Test]
        public void StartGame_FiresMatchingEventAtTurnStartAndTransitionsToShowingEvent()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventSO gameEvent = CreateEvent(
                eventId: 5, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1, skillDelta: 5);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            List<GameState> raisedStates = new List<GameState>();
            List<int> raisedEventIds = new List<int>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameEventFiredChannelSO eventFiredChannel = CreateChannel<GameEventFiredChannelSO, int>(raisedEventIds.Add);
            GameFlowController controller = CreateController(
                rules, catalog, gameStateChannel: gameStateChannel, eventFiredChannel: eventFiredChannel);

            controller.StartGame();

            Assert.AreEqual(GamePhase.ShowingEvent, controller.CurrentPhase);
            Assert.AreEqual(2, raisedStates.Count);
            Assert.AreEqual(1, raisedEventIds.Count);
            Assert.AreEqual(5, raisedEventIds[0]);
            Assert.AreEqual(5, controller.CurrentState.Skill);
        }

        [Test]
        public void ExecuteCommand_IsIgnoredWhilePhaseIsShowingEvent()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventSO gameEvent = CreateEvent(
                eventId: 5, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1, skillDelta: 5);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            controller.ExecuteCommand(train);

            Assert.AreEqual(GamePhase.ShowingEvent, controller.CurrentPhase);
            Assert.AreEqual(5, controller.CurrentState.Skill);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(2, raisedStates.Count);
        }

        [Test]
        public void OnEventDismissed_TransitionsFromShowingEventToWaitingInput()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventSO gameEvent = CreateEvent(
                eventId: 5, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1, skillDelta: 5);
            GameEventCatalogSO catalog = CreateCatalog(0, 100, gameEvent);
            GameFlowController controller = CreateController(rules, catalog);
            controller.StartGame();
            Assert.AreEqual(GamePhase.ShowingEvent, controller.CurrentPhase);

            controller.OnEventDismissed();

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
        }

        [Test]
        public void ExecuteCommand_AppliesEffectAndReturnsToWaitingInputWhenGameContinues()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            controller.ExecuteCommand(train);

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
            Assert.AreEqual(80, controller.CurrentState.Stamina);
            Assert.AreEqual(8, controller.CurrentState.Skill);
            Assert.AreEqual(45, controller.CurrentState.Mental);
            Assert.AreEqual(2, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(2, raisedStates.Count);
        }

        [Test]
        public void ExecuteCommand_IsIgnoredAndStateUnchangedWhenStaminaInsufficient()
        {
            GameRulesSO rules = CreateRules(0, 100, 10, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<GameState> raisedStates = new List<GameState>();
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>(raisedStates.Add);
            GameFlowController controller = CreateController(rules, catalog, gameStateChannel: gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            controller.ExecuteCommand(train);

            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
            Assert.AreEqual(10, controller.CurrentState.Stamina);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(1, raisedStates.Count);
        }

        [Test]
        public void ExecuteCommand_TransitionsToGameOverWithoutRaisingEndingWhenMentalReachesZero()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 24);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            List<EndingKind> raisedEndings = new List<EndingKind>();
            EndingDecidedChannelSO endingDecidedChannel = CreateChannel<EndingDecidedChannelSO, EndingKind>(raisedEndings.Add);
            GameFlowController controller = CreateController(rules, catalog, endingDecidedChannel: endingDecidedChannel);
            controller.StartGame();
            CommandDataSO breakdown = CreateCommand("Breakdown", 0, 0, -50, 0);

            controller.ExecuteCommand(breakdown);

            Assert.AreEqual(GamePhase.GameOver, controller.CurrentPhase);
            Assert.AreEqual(0, controller.CurrentState.Mental);
            Assert.AreEqual(1, controller.CurrentState.CurrentTurn);
            Assert.AreEqual(0, raisedEndings.Count);
        }

        [Test]
        public void ExecuteCommand_TransitionsToGameClearAndRaisesEndingDecidedChannelOnFinalTurn()
        {
            GameRulesSO rules = CreateRules(0, 100, 100, 0, 50, 1, 1);
            GameEventCatalogSO catalog = CreateCatalog(0, 100);
            EndingRulesSO endingRules = EndingRulesSOFactory.Create(2, 1, 1, 1000);
            _createdObjects.Add(endingRules);
            EndingResolverSO endingResolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(endingResolver);
            List<EndingKind> raisedEndings = new List<EndingKind>();
            EndingDecidedChannelSO endingDecidedChannel = CreateChannel<EndingDecidedChannelSO, EndingKind>(raisedEndings.Add);
            GameFlowController controller = CreateController(
                rules, catalog, endingRules: endingRules, endingResolver: endingResolver,
                endingDecidedChannel: endingDecidedChannel);
            controller.StartGame();
            CommandDataSO rest = CreateCommand("Rest", 0, 0, 0, 0);

            controller.ExecuteCommand(rest);

            Assert.AreEqual(GamePhase.GameClear, controller.CurrentPhase);
            Assert.AreEqual(1, raisedEndings.Count);
            Assert.AreEqual(EndingKind.Stamina, raisedEndings[0]);
        }

        private GameFlowController CreateController(
            GameRulesSO rules,
            GameEventCatalogSO catalog,
            EndingRulesSO endingRules = null,
            EndingResolverSO endingResolver = null,
            GameStateEventChannelSO gameStateChannel = null,
            GameEventFiredChannelSO eventFiredChannel = null,
            EndingDecidedChannelSO endingDecidedChannel = null)
        {
            GameObject controllerObject = new GameObject(nameof(GameFlowController));
            _createdObjects.Add(controllerObject);
            GameFlowController controller = controllerObject.AddComponent<GameFlowController>();

            CommandResolverSO commandResolver = ScriptableObject.CreateInstance<CommandResolverSO>();
            _createdObjects.Add(commandResolver);
            EventResolverSO eventResolver = ScriptableObject.CreateInstance<EventResolverSO>();
            _createdObjects.Add(eventResolver);

            SetField(controller, "_gameRules", rules);
            SetField(controller, "_commandResolver", commandResolver);
            SetField(controller, "_eventCatalog", catalog);
            SetField(controller, "_eventResolver", eventResolver);
            SetField(controller, "_endingRules", endingRules ?? EmptyEndingRules());
            SetField(controller, "_endingResolver", endingResolver ?? EmptyEndingResolver());
            SetField(controller, "_gameStateChannel", gameStateChannel ?? CreateChannel<GameStateEventChannelSO, GameState>(null));
            SetField(controller, "_eventFiredChannel", eventFiredChannel ?? CreateChannel<GameEventFiredChannelSO, int>(null));
            SetField(controller, "_endingDecidedChannel", endingDecidedChannel ?? CreateChannel<EndingDecidedChannelSO, EndingKind>(null));

            return controller;
        }

        private EndingRulesSO EmptyEndingRules()
        {
            EndingRulesSO rules = EndingRulesSOFactory.Create(2, 1, 1, 250);
            _createdObjects.Add(rules);
            return rules;
        }

        private EndingResolverSO EmptyEndingResolver()
        {
            EndingResolverSO resolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(resolver);
            return resolver;
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

        private GameEventCatalogSO CreateCatalog(int paramMin, int paramMax, params GameEventSO[] events)
        {
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(paramMin, paramMax, events);
            _createdObjects.Add(catalog);
            return catalog;
        }

        private GameEventSO CreateEvent(
            int eventId, EventTriggerKind triggerKind, int triggerTurn,
            TrackedParameter targetParameter, int threshold, int priority,
            int staminaDelta = 0, int skillDelta = 0, int mentalDelta = 0)
        {
            GameEventSO gameEvent = GameEventSOFactory.CreateEvent(
                eventId, triggerKind, triggerTurn, targetParameter, threshold, priority,
                staminaDelta, skillDelta, mentalDelta);
            _createdObjects.Add(gameEvent);
            return gameEvent;
        }

        private CommandDataSO CreateCommand(
            string commandName, int staminaDelta, int skillDelta, int mentalDelta, int staminaCost)
        {
            CommandDataSO command = ScriptableObject.CreateInstance<CommandDataSO>();
            SetField(command, "_commandName", commandName);
            SetField(command, "_staminaDelta", staminaDelta);
            SetField(command, "_skillDelta", skillDelta);
            SetField(command, "_mentalDelta", mentalDelta);
            SetField(command, "_staminaCost", staminaCost);
            _createdObjects.Add(command);
            return command;
        }

        private TChannel CreateChannel<TChannel, TValue>(Action<TValue> onRaised)
            where TChannel : EventChannelSO<TValue>
        {
            TChannel channel = ScriptableObject.CreateInstance<TChannel>();
            _createdObjects.Add(channel);

            if (onRaised != null)
            {
                channel.OnEventRaised += onRaised;
            }

            return channel;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。" +
                    "フィールド名がリネームされていないか、テストヘルパーを確認してください。");
            }

            field.SetValue(target, value);
        }
    }
}
