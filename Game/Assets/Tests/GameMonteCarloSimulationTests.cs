// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.Relic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GameMonteCarloSimulationTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
        private readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _createdGameObjects)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _createdGameObjects.Clear();

            foreach (UnityEngine.Object obj in _createdObjects)
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void MonteCarlo_1000Runs_RandomPolicy_RunsWithoutExceptionsAndProducesValidOutcomes()
        {
            GameRulesSO rules = CreateRules();
            CommandDataSO rest = CreateCommand("Rest", staminaDelta: 20, skillDelta: 0, mentalDelta: 10, staminaCost: 0);
            CommandDataSO train = CreateCommand("Train", staminaDelta: -20, skillDelta: 8, mentalDelta: -5, staminaCost: 20);
            CommandDataSO special = CreateCommand("Special", staminaDelta: -35, skillDelta: 16, mentalDelta: -10, staminaCost: 35);
            CommandDataSO[] commands = { rest, train, special };

            RelicSO r1 = CreateRelic(1, RelicTriggerKind.OnTurnStart, 2, 0, 0);
            RelicSO r2 = CreateRelic(2, RelicTriggerKind.OnCommandExecuted, 0, 3, 0);
            RelicCatalogSO catalog = CreateCatalog(r1, r2);
            RelicResolverSO relicResolver = ScriptableObject.CreateInstance<RelicResolverSO>();
            _createdObjects.Add(relicResolver);

            int clearCount = 0;
            int gameOverCount = 0;
            int totalFinalSkill = 0;

            System.Random rand = new System.Random(42); // 固定シードで決定論的検証

            for (int run = 0; run < 1000; run++)
            {
                GameFlowController controller = CreateController(rules, catalog, relicResolver);
                controller.StartGame();

                // 24ターン分シミュレーション
                for (int t = 0; t < 30; t++)
                {
                    if (controller.CurrentPhase == GamePhase.GameClear || controller.CurrentPhase == GamePhase.GameOver)
                    {
                        break;
                    }

                    if (controller.CurrentPhase == GamePhase.ShowingEvent)
                    {
                        controller.OnEventDismissed();
                    }

                    if (controller.CurrentPhase == GamePhase.WaitingInput)
                    {
                        // ランダムにコマンドを選択（実行可能なものから選択）
                        CommandDataSO chosen = commands[rand.Next(commands.Length)];
                        controller.ExecuteCommand(chosen);

                        // 実行不可だった場合は休養
                        if (controller.CurrentPhase == GamePhase.WaitingInput)
                        {
                            controller.ExecuteCommand(rest);
                        }
                    }
                }

                if (controller.CurrentPhase == GamePhase.GameClear)
                {
                    clearCount++;
                    totalFinalSkill += controller.CurrentState.Skill;
                }
                else if (controller.CurrentPhase == GamePhase.GameOver)
                {
                    gameOverCount++;
                }
            }

            Assert.AreEqual(1000, clearCount + gameOverCount, "1000 回のシミュレーションがすべて正常にクリアまたはゲームオーバーで終了すること");
            Debug.Log($"[MonteCarlo] 1,000 Runs Completed: Clear={clearCount}, GameOver={gameOverCount}, AvgSkillWhenCleared={(clearCount > 0 ? (float)totalFinalSkill / clearCount : 0):F1}");
        }

        private GameRulesSO CreateRules()
        {
            GameRulesSO rules = GameRulesSOFactory.Create(
                paramMin: 0, paramMax: 100, initialStamina: 100, initialSkill: 0, initialMental: 50, startTurn: 1, maxTurn: 24);
            _createdObjects.Add(rules);
            return rules;
        }

        private CommandDataSO CreateCommand(string name, int staminaDelta, int skillDelta, int mentalDelta, int staminaCost)
        {
            CommandDataSO cmd = ScriptableObject.CreateInstance<CommandDataSO>();
            SetField(cmd, "_commandName", name);
            SetField(cmd, "_staminaDelta", staminaDelta);
            SetField(cmd, "_skillDelta", skillDelta);
            SetField(cmd, "_mentalDelta", mentalDelta);
            SetField(cmd, "_staminaCost", staminaCost);
            _createdObjects.Add(cmd);
            return cmd;
        }

        private RelicSO CreateRelic(int id, RelicTriggerKind trigger, int stam, int skill, int men)
        {
            RelicSO relic = ScriptableObject.CreateInstance<RelicSO>();
            SetField(relic, "_relicId", id);
            SetField(relic, "_displayName", $"Relic_{id}");
            SetField(relic, "_description", $"Desc_{id}");
            SetField(relic, "_triggerKind", trigger);
            SetField(relic, "_staminaDeltaBonus", stam);
            SetField(relic, "_skillDeltaBonus", skill);
            SetField(relic, "_mentalDeltaBonus", men);
            SetField(relic, "_staminaCostMultiplier", 1f);
            _createdObjects.Add(relic);
            return relic;
        }

        private RelicCatalogSO CreateCatalog(params RelicSO[] relics)
        {
            RelicCatalogSO cat = ScriptableObject.CreateInstance<RelicCatalogSO>();
            SetField(cat, "_relics", new List<RelicSO>(relics));
            _createdObjects.Add(cat);
            return cat;
        }

        private GameFlowController CreateController(GameRulesSO rules, RelicCatalogSO catalog, RelicResolverSO relicResolver)
        {
            GameObject go = new GameObject("GameFlowController");
            _createdGameObjects.Add(go);
            GameFlowController controller = go.AddComponent<GameFlowController>();

            CommandResolverSO commandResolver = ScriptableObject.CreateInstance<CommandResolverSO>();
            _createdObjects.Add(commandResolver);

            EventResolverSO eventResolver = ScriptableObject.CreateInstance<EventResolverSO>();
            _createdObjects.Add(eventResolver);

            GameEventCatalogSO eventCatalog = ScriptableObject.CreateInstance<GameEventCatalogSO>();
            SetField(eventCatalog, "_events", new List<GameEventSO>());
            _createdObjects.Add(eventCatalog);

            EndingResolverSO endingResolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(endingResolver);

            EndingRulesSO endingRules = ScriptableObject.CreateInstance<EndingRulesSO>();
            _createdObjects.Add(endingRules);

            GameStateEventChannelSO gameStateChannel = ScriptableObject.CreateInstance<GameStateEventChannelSO>();
            _createdObjects.Add(gameStateChannel);

            GameEventFiredChannelSO eventFiredChannel = ScriptableObject.CreateInstance<GameEventFiredChannelSO>();
            _createdObjects.Add(eventFiredChannel);

            EndingDecidedChannelSO endingDecidedChannel = ScriptableObject.CreateInstance<EndingDecidedChannelSO>();
            _createdObjects.Add(endingDecidedChannel);

            SetField(controller, "_gameRules", rules);
            SetField(controller, "_commandResolver", commandResolver);
            SetField(controller, "_eventCatalog", eventCatalog);
            SetField(controller, "_eventResolver", eventResolver);
            SetField(controller, "_endingRules", endingRules);
            SetField(controller, "_endingResolver", endingResolver);
            SetField(controller, "_relicCatalog", catalog);
            SetField(controller, "_relicResolver", relicResolver);
            SetField(controller, "_gameStateChannel", gameStateChannel);
            SetField(controller, "_eventFiredChannel", eventFiredChannel);
            SetField(controller, "_endingDecidedChannel", endingDecidedChannel);

            return controller;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FieldFlags);
            if (field == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} にフィールド '{fieldName}' が見つかりません。");
            }
            field.SetValue(target, value);
        }
    }
}
