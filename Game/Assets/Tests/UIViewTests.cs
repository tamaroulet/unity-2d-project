// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// StatusView, CommandButtonView, EventDialogView, EndingView の
    /// イベント受信・UI更新・表示切り替えを検証する。
    /// </summary>
    public class UIViewTests
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        // TextMeshProUGUI は TMP_FontAsset が未設定のままだと文字ジオメトリを生成できず、
        // text ゲッターが空文字を返してしまう。本プロジェクトには TMP Essential Resources が
        // インポートされておらず TMP_Settings.defaultFontAsset も null のため、
        // テスト実行時に動的フォントから生成したフォントアセットを全テストで使い回す。
        private TMP_FontAsset _testFontAsset;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testFontAsset = TMP_FontAsset.CreateFontAsset(Font.CreateDynamicFontFromOSFont("Arial", 16));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_testFontAsset != null)
            {
                UnityEngine.Object.DestroyImmediate(_testFontAsset);
                _testFontAsset = null;
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object createdObject in _createdObjects)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        // ---- StatusView ----

        [Test]
        public void StatusView_OnGameStateChanged_UpdatesTextsAndGauges()
        {
            GameStateEventChannelSO channel = CreateChannel<GameStateEventChannelSO, GameState>();
            GameObject viewObject = CreateInactiveGameObject(nameof(StatusView));
            StatusView view = viewObject.AddComponent<StatusView>();
            TextMeshProUGUI turnText = CreateText(viewObject);
            TextMeshProUGUI staminaText = CreateText(viewObject);
            TextMeshProUGUI skillText = CreateText(viewObject);
            TextMeshProUGUI mentalText = CreateText(viewObject);
            Slider staminaGauge = CreateSlider(viewObject);
            Slider skillGauge = CreateSlider(viewObject);
            Slider mentalGauge = CreateSlider(viewObject);

            SetField(view, "_gameStateChannel", channel);
            SetField(view, "_turnText", turnText);
            SetField(view, "_staminaText", staminaText);
            SetField(view, "_skillText", skillText);
            SetField(view, "_mentalText", mentalText);
            SetField(view, "_staminaGauge", staminaGauge);
            SetField(view, "_skillGauge", skillGauge);
            SetField(view, "_mentalGauge", mentalGauge);

            viewObject.SetActive(true);
            view.Bind(channel);

            channel.Raise(new GameState { CurrentTurn = 3, Stamina = 70, Skill = 40, Mental = 55 });

            Assert.AreEqual(3, view.LastDisplayedState.CurrentTurn);
            Assert.AreEqual(70, view.LastDisplayedState.Stamina);
            Assert.AreEqual(40, view.LastDisplayedState.Skill);
            Assert.AreEqual(55, view.LastDisplayedState.Mental);
            Assert.AreEqual(70f, staminaGauge.value);
            Assert.AreEqual(40f, skillGauge.value);
            Assert.AreEqual(55f, mentalGauge.value);
        }

        [Test]
        public void StatusView_OnDisable_StopsReceivingUpdates()
        {
            GameStateEventChannelSO channel = CreateChannel<GameStateEventChannelSO, GameState>();
            GameObject viewObject = CreateInactiveGameObject(nameof(StatusView));
            StatusView view = viewObject.AddComponent<StatusView>();
            TextMeshProUGUI turnText = CreateText(viewObject);
            SetField(view, "_gameStateChannel", channel);
            SetField(view, "_turnText", turnText);
            viewObject.SetActive(true);
            turnText.text = "unchanged";

            viewObject.SetActive(false);
            channel.Raise(new GameState { CurrentTurn = 9 });

            Assert.AreEqual("unchanged", turnText.text);
        }

        // ---- CommandButtonView ----

        [Test]
        public void CommandButtonView_OnEnable_DisplaysCommandNameAndCost()
        {
            CommandDataSO command = CreateCommand("Train", -20, 8, -5, 20);
            GameObject viewObject = CreateInactiveGameObject(nameof(CommandButtonView));
            CommandButtonView view = viewObject.AddComponent<CommandButtonView>();
            Button button = viewObject.AddComponent<Button>();
            TextMeshProUGUI nameText = CreateText(viewObject);
            TextMeshProUGUI costText = CreateText(viewObject);

            SetField(view, "_command", command);
            SetField(view, "_button", button);
            SetField(view, "_nameText", nameText);
            SetField(view, "_costText", costText);

            viewObject.SetActive(true);

            Assert.AreEqual("Train", view.DisplayedName);
            Assert.AreEqual(20, view.DisplayedCost);
        }

        [Test]
        public void CommandButtonView_OnCommandClick_InvokesGameFlowControllerExecuteCommand()
        {
            GameRulesSO rules = GameRulesSOFactory.Create(0, 100, 100, 0, 50, 1, 24);
            _createdObjects.Add(rules);
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(0, 100);
            _createdObjects.Add(catalog);
            GameStateEventChannelSO gameStateChannel = CreateChannel<GameStateEventChannelSO, GameState>();
            GameFlowController controller = CreateFlowController(rules, catalog, gameStateChannel);
            controller.StartGame();
            CommandDataSO train = CreateCommand("Train", -20, 8, -5, 20);

            GameObject viewObject = CreateInactiveGameObject(nameof(CommandButtonView));
            CommandButtonView view = viewObject.AddComponent<CommandButtonView>();
            Button button = viewObject.AddComponent<Button>();
            SetField(view, "_command", train);
            SetField(view, "_gameFlowController", controller);
            SetField(view, "_button", button);
            viewObject.SetActive(true);

            view.Execute();

            Assert.AreEqual(80, controller.CurrentState.Stamina);
            Assert.AreEqual(8, controller.CurrentState.Skill);
            Assert.AreEqual(45, controller.CurrentState.Mental);
            Assert.AreEqual(GamePhase.WaitingInput, controller.CurrentPhase);
        }

        // ---- EventDialogView ----

        [Test]
        public void EventDialogView_OnEventFired_ShowsPanelWithMatchingEventContent()
        {
            GameEventSO gameEvent = GameEventSOFactory.CreateEvent(
                eventId: 3, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1,
                displayName: "特訓の誘い", body: "先輩から特訓に誘われた。");
            _createdObjects.Add(gameEvent);
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(0, 100, gameEvent);
            _createdObjects.Add(catalog);
            GameEventFiredChannelSO channel = CreateChannel<GameEventFiredChannelSO, int>();

            GameObject viewObject = CreateInactiveGameObject(nameof(EventDialogView));
            EventDialogView view = viewObject.AddComponent<EventDialogView>();
            GameObject panel = new GameObject("Panel");
            panel.SetActive(false);
            _createdObjects.Add(panel);
            TextMeshProUGUI titleText = CreateText(viewObject);
            TextMeshProUGUI bodyText = CreateText(viewObject);
            Button okButton = viewObject.AddComponent<Button>();

            SetField(view, "_eventFiredChannel", channel);
            SetField(view, "_eventCatalog", catalog);
            SetField(view, "_panelRoot", panel);
            SetField(view, "_titleText", titleText);
            SetField(view, "_bodyText", bodyText);
            SetField(view, "_okButton", okButton);

            viewObject.SetActive(true);
            view.Bind(channel, catalog);

            Assert.IsFalse(view.IsPanelActive);

            channel.Raise(3);

            Assert.IsTrue(view.IsPanelActive);
            Assert.AreEqual("特訓の誘い", view.DisplayedTitle);
            Assert.AreEqual("先輩から特訓に誘われた。", bodyText.text);
        }

        [Test]
        public void EventDialogView_OnEventFired_IgnoresUnknownEventId()
        {
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(0, 100);
            _createdObjects.Add(catalog);
            GameEventFiredChannelSO channel = CreateChannel<GameEventFiredChannelSO, int>();

            GameObject viewObject = CreateInactiveGameObject(nameof(EventDialogView));
            EventDialogView view = viewObject.AddComponent<EventDialogView>();
            GameObject panel = new GameObject("Panel");
            panel.SetActive(false);
            _createdObjects.Add(panel);

            SetField(view, "_eventFiredChannel", channel);
            SetField(view, "_eventCatalog", catalog);
            SetField(view, "_panelRoot", panel);

            viewObject.SetActive(true);

            Assert.DoesNotThrow(() => channel.Raise(99));
            Assert.IsFalse(view.IsPanelActive);
        }

        [Test]
        public void EventDialogView_OnEventDismissed_HidesPanel()
        {
            GameEventSO gameEvent = GameEventSOFactory.CreateEvent(
                eventId: 3, triggerKind: EventTriggerKind.TurnReached, triggerTurn: 1,
                targetParameter: TrackedParameter.Skill, threshold: 0, priority: 1,
                displayName: "特訓の誘い", body: "先輩から特訓に誘われた。");
            _createdObjects.Add(gameEvent);
            GameEventCatalogSO catalog = GameEventSOFactory.CreateCatalog(0, 100, gameEvent);
            _createdObjects.Add(catalog);
            GameEventFiredChannelSO channel = CreateChannel<GameEventFiredChannelSO, int>();

            GameObject viewObject = CreateInactiveGameObject(nameof(EventDialogView));
            EventDialogView view = viewObject.AddComponent<EventDialogView>();
            GameObject panel = new GameObject("Panel");
            panel.SetActive(false);
            _createdObjects.Add(panel);
            Button okButton = viewObject.AddComponent<Button>();

            SetField(view, "_eventFiredChannel", channel);
            SetField(view, "_eventCatalog", catalog);
            SetField(view, "_panelRoot", panel);
            SetField(view, "_okButton", okButton);

            viewObject.SetActive(true);
            view.Bind(channel, catalog);
            channel.Raise(3);
            Assert.IsTrue(view.IsPanelActive);

            view.Dismiss();

            Assert.IsFalse(view.IsPanelActive);
        }

        // ---- BossBattleDialogView ----

        [Test]
        public void BossBattleDialogView_Show_SetsPanelActive_AndUpdatesTextsAndGauges()
        {
            GameObject viewObject = CreateInactiveGameObject(nameof(BossBattleDialogView));
            BossBattleDialogView view = viewObject.AddComponent<BossBattleDialogView>();
            GameObject panelRoot = new GameObject("PanelRoot");
            panelRoot.SetActive(false);
            _createdObjects.Add(panelRoot);

            TextMeshProUGUI bossNameText = CreateText(viewObject);
            TextMeshProUGUI bossHpText = CreateText(viewObject);
            Slider bossHpSlider = CreateSlider(viewObject);
            TextMeshProUGUI shieldText = CreateText(viewObject);
            TextMeshProUGUI battleLogText = CreateText(viewObject);
            Button dismissButton = viewObject.AddComponent<Button>();

            SetField(view, "_panelRoot", panelRoot);
            SetField(view, "_bossNameText", bossNameText);
            SetField(view, "_bossHpText", bossHpText);
            SetField(view, "_bossHpSlider", bossHpSlider);
            SetField(view, "_shieldText", shieldText);
            SetField(view, "_battleLogText", battleLogText);
            SetField(view, "_dismissButton", dismissButton);

            viewObject.SetActive(true);

            BossSO boss = ScriptableObject.CreateInstance<BossSO>();
            _createdObjects.Add(boss);
            SetField(boss, "_bossId", 1);
            SetField(boss, "_bossName", "Boss_Act1_01");
            SetField(boss, "_maxHp", 100);

            BossState finalBoss = new BossState { BossId = 1, CurrentHp = 0, MaxHp = 100, Shield = 0, BattleTurn = 3 };
            GameState finalPlayer = new GameState { CurrentTurn = 12, Stamina = 60, Skill = 30, Mental = 70 };
            FullBattleResult battleResult = new FullBattleResult(
                BattleOutcomeKind.Victory, finalPlayer, finalBoss, new List<BattleTurnResult>());

            view.Show(boss, battleResult);

            Assert.IsTrue(panelRoot.activeSelf, "panelRoot should be active after Show");
            Assert.AreEqual("Boss_Act1_01", bossNameText.text);
            Assert.AreEqual("HP: 0 / 100", bossHpText.text);
            Assert.AreEqual(0f, bossHpSlider.value);
            Assert.IsTrue(battleLogText.text.Contains("[VICTORY]"));
        }

        [Test]
        public void BossBattleDialogView_Dismiss_HidesPanelAndInvokesCallback()
        {
            GameObject viewObject = CreateInactiveGameObject(nameof(BossBattleDialogView));
            BossBattleDialogView view = viewObject.AddComponent<BossBattleDialogView>();
            GameObject panelRoot = new GameObject("PanelRoot");
            panelRoot.SetActive(false);
            _createdObjects.Add(panelRoot);

            Button dismissButton = viewObject.AddComponent<Button>();
            SetField(view, "_panelRoot", panelRoot);
            SetField(view, "_dismissButton", dismissButton);

            viewObject.SetActive(true);

            BossSO boss = ScriptableObject.CreateInstance<BossSO>();
            _createdObjects.Add(boss);
            BossState finalBoss = new BossState { BossId = 1, CurrentHp = 0, MaxHp = 100, Shield = 0, BattleTurn = 1 };
            GameState finalPlayer = new GameState { CurrentTurn = 12, Stamina = 60, Skill = 30, Mental = 70 };
            FullBattleResult battleResult = new FullBattleResult(
                BattleOutcomeKind.Victory, finalPlayer, finalBoss, new List<BattleTurnResult>());

            bool dismissed = false;
            view.Show(boss, battleResult, () => dismissed = true);

            view.Dismiss();

            Assert.IsTrue(dismissed, "dismiss callback should be invoked");
            Assert.IsFalse(panelRoot.activeSelf, "panelRoot should be inactive after dismiss");
        }

        // ---- EndingView ----

        [Test]
        public void EndingView_OnEndingDecided_ShowsPanelWithResultText()
        {
            EndingDecidedChannelSO channel = CreateChannel<EndingDecidedChannelSO, EndingKind>();
            GameObject viewObject = CreateInactiveGameObject(nameof(EndingView));
            EndingView view = viewObject.AddComponent<EndingView>();
            GameObject panel = new GameObject("Panel");
            panel.SetActive(false);
            _createdObjects.Add(panel);
            TextMeshProUGUI resultText = CreateText(viewObject);

            SetField(view, "_endingDecidedChannel", channel);
            SetField(view, "_panelRoot", panel);
            SetField(view, "_resultText", resultText);

            viewObject.SetActive(true);
            view.Bind(channel);

            Assert.IsFalse(view.IsPanelActive);

            channel.Raise(EndingKind.Skill);

            Assert.IsTrue(view.IsPanelActive);
            Assert.AreEqual("Skill", view.DisplayedResult);
        }

        [Test]
        public void EndingView_OnDisable_StopsReceivingUpdates()
        {
            EndingDecidedChannelSO channel = CreateChannel<EndingDecidedChannelSO, EndingKind>();
            GameObject viewObject = CreateInactiveGameObject(nameof(EndingView));
            EndingView view = viewObject.AddComponent<EndingView>();
            GameObject panel = new GameObject("Panel");
            panel.SetActive(false);
            _createdObjects.Add(panel);

            SetField(view, "_endingDecidedChannel", channel);
            SetField(view, "_panelRoot", panel);

            viewObject.SetActive(true);
            viewObject.SetActive(false);

            channel.Raise(EndingKind.True);

            Assert.IsFalse(panel.activeSelf);
        }

        // ---- Helpers ----

        private GameFlowController CreateFlowController(
            GameRulesSO rules, GameEventCatalogSO catalog, GameStateEventChannelSO gameStateChannel)
        {
            GameObject controllerObject = new GameObject(nameof(GameFlowController));
            _createdObjects.Add(controllerObject);
            GameFlowController controller = controllerObject.AddComponent<GameFlowController>();

            CommandResolverSO commandResolver = ScriptableObject.CreateInstance<CommandResolverSO>();
            _createdObjects.Add(commandResolver);
            EventResolverSO eventResolver = ScriptableObject.CreateInstance<EventResolverSO>();
            _createdObjects.Add(eventResolver);
            EndingRulesSO endingRules = EndingRulesSOFactory.Create(2, 1, 1, 250);
            _createdObjects.Add(endingRules);
            EndingResolverSO endingResolver = ScriptableObject.CreateInstance<EndingResolverSO>();
            _createdObjects.Add(endingResolver);

            SetField(controller, "_gameRules", rules);
            SetField(controller, "_commandResolver", commandResolver);
            SetField(controller, "_eventCatalog", catalog);
            SetField(controller, "_eventResolver", eventResolver);
            SetField(controller, "_endingRules", endingRules);
            SetField(controller, "_endingResolver", endingResolver);
            SetField(controller, "_gameStateChannel", gameStateChannel);
            SetField(controller, "_eventFiredChannel", CreateChannel<GameEventFiredChannelSO, int>());
            SetField(controller, "_endingDecidedChannel", CreateChannel<EndingDecidedChannelSO, EndingKind>());

            return controller;
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

        private GameObject CreateInactiveGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.SetActive(false);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private TextMeshProUGUI CreateText(GameObject parent)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent.transform, false);
            _createdObjects.Add(textObject);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            // フォントアセット未設定だと text ゲッターが空文字を返すため、テスト用フォントを設定する。
            text.font = _testFontAsset;
            // AddComponent 直後は m_text が未初期化(null)のため、明示的に空文字へ初期化する。
            text.text = string.Empty;
            return text;
        }

        private Slider CreateSlider(GameObject parent)
        {
            GameObject sliderObject = new GameObject("Slider");
            sliderObject.transform.SetParent(parent.transform);
            _createdObjects.Add(sliderObject);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            return slider;
        }

        private TChannel CreateChannel<TChannel, TValue>()
            where TChannel : EventChannelSO<TValue>
        {
            TChannel channel = ScriptableObject.CreateInstance<TChannel>();
            _createdObjects.Add(channel);
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
