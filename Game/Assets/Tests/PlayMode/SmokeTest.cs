// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.GameFlow;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Gate 4 の曳光弾（tracer bullet）。MainGame シーンを実際にロードし、実際の uGUI
    /// クリック経路で STUDY コマンドを 1 回実行して、TURN 1 → 2 の進行・ゲージ
    /// (BarFill) の変動・例外 0 件を 1 本で証明する。
    ///
    /// このテストが赤になっても、緑にするためにプロダクションコードへ分岐・自己修復・
    /// Find 系の再導入を行ってはならない（00_rules.md「テスト」節）。出力をそのまま
    /// 貼って停止し、人間に報告すること。
    ///
    /// ゲージは Slider ではなく RectTransform.anchorMax.x で表現されている
    /// （StatusView._staminaGauge 等はシーン上で未アサインであり、Slider は存在しない）。
    /// </summary>
    public class SmokeTest
    {
        private const string SceneName = "MainGame";
        private const string StudyButtonName = "StudyButton";
        private const float TimeoutSeconds = 10f;
        private const float FillTolerance = 0.001f;

        private readonly List<string> _capturedFailures = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _capturedFailures.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _capturedFailures.Add($"[{type}] {condition}\n{stackTrace}");
            }
        }

        [UnityTest]
        public IEnumerator MainGame_StudyButtonClick_AdvancesTurnFrom1To2_WithZeroExceptions()
        {
            LogAssert.Expect(LogType.Log, "[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50");
            LogAssert.Expect(LogType.Log, "[CommandButtonView] Clicked button for command: Study");
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"^\[SmokeTest\] BEFORE.*"));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"^\[SmokeTest\] AFTER.*"));

            // ---------- Arrange: MainGame をロードし、入力待ちまで進める ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(
                load != null,
                $"シーン '{SceneName}' のロードを開始できなかった。EditorBuildSettings の Scene List を確認せよ。");

            while (!load.isDone)
            {
                yield return null;
            }

            // Awake / OnEnable / Start を確実に 1 巡させる
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            StatusView status = UnityEngine.Object.FindFirstObjectByType<StatusView>();
            Assert.IsTrue(status != null, "StatusView が MainGame シーンに存在しない。");

            yield return WaitForCondition(
                () => flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"起動から {TimeoutSeconds} 秒以内に GamePhase.WaitingInput へ到達しなかった。"
                      + $" 現在の Phase = {flow.CurrentPhase}");

            GameState before = flow.CurrentState;
            Assert.IsTrue(before != null, "StartGame 後に GameFlowController.CurrentState が null。");
            Assert.AreEqual(1, before.CurrentTurn, "初期ターンが 1 ではない。");
            Assert.IsTrue(
                status.LastDisplayedState != null,
                "StatusView が初期 GameState を受信していない。GameStateChannel の購読経路が切れている。");

            RectTransform staminaBar = GetSerializedField<RectTransform>(status, "_staminaBarFill");
            RectTransform skillBar = GetSerializedField<RectTransform>(status, "_skillBarFill");
            RectTransform mentalBar = GetSerializedField<RectTransform>(status, "_mentalBarFill");
            TextMeshProUGUI turnText = GetSerializedField<TextMeshProUGUI>(status, "_turnText");

            float staminaFillBefore = staminaBar.anchorMax.x;
            float skillFillBefore = skillBar.anchorMax.x;
            float mentalFillBefore = mentalBar.anchorMax.x;

            CommandButtonView studyView = FindCommandButtonByName(StudyButtonName);
            Button studyButton = studyView.GetComponent<Button>();
            Assert.IsTrue(studyButton != null, $"'{StudyButtonName}' に Button コンポーネントが無い。");
            Assert.IsTrue(studyButton.IsInteractable(), $"'{StudyButtonName}' が interactable でない。");

            Debug.Log(
                $"[SmokeTest] BEFORE Turn={before.CurrentTurn} Stamina={before.Stamina}"
                + $" Skill={before.Skill} Mental={before.Mental}"
                + $" Fill(Sta/Skl/Mnt)={staminaFillBefore:F3}/{skillFillBefore:F3}/{mentalFillBefore:F3}"
                + $" TurnText=\"{turnText.text}\" Phase={flow.CurrentPhase}");

            // ---------- Act: 実際の uGUI クリック経路をエミュレートする ----------
            bool accepted = ExecuteEvents.Execute(
                studyButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(accepted, $"'{StudyButtonName}' が pointerClick を受理しなかった。");

            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn >= 2,
                () => $"クリックから {TimeoutSeconds} 秒以内に TURN が 2 へ進まなかった。"
                      + $" 現在の Turn = {(flow.CurrentState != null ? flow.CurrentState.CurrentTurn : -1)},"
                      + $" Phase = {flow.CurrentPhase}");

            // UI 反映を確定させるため 1 フレーム待つ
            yield return null;

            // ---------- Assert ----------
            GameState after = flow.CurrentState;
            GameState displayed = status.LastDisplayedState;

            float staminaFillAfter = staminaBar.anchorMax.x;
            float skillFillAfter = skillBar.anchorMax.x;
            float mentalFillAfter = mentalBar.anchorMax.x;

            Debug.Log(
                $"[SmokeTest] AFTER  Turn={after.CurrentTurn} Stamina={after.Stamina}"
                + $" Skill={after.Skill} Mental={after.Mental}"
                + $" Fill(Sta/Skl/Mnt)={staminaFillAfter:F3}/{skillFillAfter:F3}/{mentalFillAfter:F3}"
                + $" TurnText=\"{turnText.text}\" Phase={flow.CurrentPhase}");

            // 1. ターン進行
            Assert.AreEqual(2, after.CurrentTurn, "TURN が 2 になっていない。");
            Assert.AreEqual(
                GamePhase.WaitingInput, flow.CurrentPhase, "TURN 2 開始後に入力待ちへ戻っていない。");

            // 2. STUDY の効果（Stamina 100→90 / Skill 0→5 / Mental 50→55）
            Assert.AreEqual(90, after.Stamina, "Stamina が 90 でない。");
            Assert.AreEqual(5, after.Skill, "Skill が 5 でない。");
            Assert.AreEqual(55, after.Mental, "Mental が 55 でない。");

            // 3. GameStateChannel 経由で View まで届いていること
            Assert.IsTrue(displayed != null, "StatusView が GameState を受信していない。");
            Assert.AreEqual(2, displayed.CurrentTurn, "StatusView が受信した Turn が 2 でない。");
            Assert.AreEqual(90, displayed.Stamina, "StatusView が受信した Stamina が 90 でない。");
            Assert.AreEqual(5, displayed.Skill, "StatusView が受信した Skill が 5 でない。");
            Assert.AreEqual(55, displayed.Mental, "StatusView が受信した Mental が 55 でない。");

            // 4. ゲージ（BarFill の anchorMax.x）が state に追随して変動したこと
            Assert.AreEqual(0.90f, staminaFillAfter, FillTolerance, "Stamina ゲージが 0.90 でない。");
            Assert.AreEqual(0.05f, skillFillAfter, FillTolerance, "Skill ゲージが 0.05 でない。");
            Assert.AreEqual(0.55f, mentalFillAfter, FillTolerance, "Mental ゲージが 0.55 でない。");
            Assert.Less(staminaFillAfter, staminaFillBefore, "Stamina ゲージが減っていない。");
            Assert.Greater(skillFillAfter, skillFillBefore, "Skill ゲージが増えていない。");
            Assert.Greater(mentalFillAfter, mentalFillBefore, "Mental ゲージが増えていない。");

            // 5. 画面ラベル
            Assert.AreEqual("TURN 2 / 24", turnText.text, "TurnText の表示が更新されていない。");

            // 6. 例外 0 件
            Assert.IsEmpty(
                _capturedFailures,
                "実行中に Error / Exception / Assert ログが発生した:\n"
                + string.Join("\n", _capturedFailures));

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MainGame_AdvanceToTurn6_BossBattleDismiss_AdvancesToTurn7_WithZeroExceptions()
        {
            // ---------- Arrange: MainGame をロード ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"シーン '{SceneName}' のロードを開始できなかった。");
            while (!load.isDone) yield return null;
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            BossBattleDialogView bossDialog = UnityEngine.Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(bossDialog != null, "BossBattleDialogView が MainGame シーンに存在しない。");

            CommandButtonView studyView = FindCommandButtonByName(StudyButtonName);
            Button studyButton = studyView.GetComponent<Button>();

            // ターン 1 から 5 まで Study コマンドを連続実行してターン 6（ボス戦）へ進める
            for (int t = 1; t <= 5; t++)
            {
                int currentTurn = t;
                yield return WaitForCondition(
                    () => flow.CurrentPhase == GamePhase.WaitingInput && flow.CurrentState.CurrentTurn == currentTurn,
                    () => $"ターン {currentTurn} の WaitingInput に到達しなかった。Phase={flow.CurrentPhase}");

                bool clicked = ExecuteEvents.Execute(
                    studyButton.gameObject,
                    new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
                Assert.IsTrue(clicked, $"ターン {currentTurn} で '{StudyButtonName}' のクリックが受理されなかった。");

                yield return null;
            }

            // ---------- Act: ターン 6（ボス戦）への突入とダイアログ確認 ----------
            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn == 6,
                () => $"ターン 6 に進まなかった。CurrentTurn={flow.CurrentState?.CurrentTurn}, Phase={flow.CurrentPhase}");

            yield return WaitForCondition(
                () => bossDialog.IsVisible,
                () => $"ターン 6 到達後に BossBattleDialogView が表示されなかった。Phase={flow.CurrentPhase}");

            Button dismissButton = GetSerializedField<Button>(bossDialog, "_dismissButton");
            Assert.IsTrue(dismissButton != null, "BossBattleDialogView._dismissButton が null。");
            Assert.IsTrue(dismissButton.IsInteractable(), "BossBattleDialogView._dismissButton が interactable でない。");

            // ボス戦ダイアログの決定（Dismiss）ボタンをクリック
            AssertRaycastReachesButton(dismissButton, "dismissButton");
            bool dismissed = ExecuteEvents.Execute(
                dismissButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(dismissed, "BossBattleDialogView の dismissButton クリックが受理されなかった。");

            // ボス勝利後、レリックドラフト画面が表示されることを確認
            RelicDraftDialogView relicDraftDialog = UnityEngine.Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(relicDraftDialog != null, "RelicDraftDialogView がシーンに見つからない。");
            yield return WaitForCondition(
                () => relicDraftDialog.IsVisible,
                () => $"ボス戦ダイアログ決定後に RelicDraftDialogView が表示されなかった。Phase={flow.CurrentPhase}");

            // カード1を選択してドラフトを完了する
            List<RelicCardView> cards = GetField<List<RelicCardView>>(relicDraftDialog, "_cardViews");
            Assert.IsTrue(cards != null && cards.Count > 0, "RelicDraftDialogView._cardViews が空。");
            Button selectButton = GetSerializedField<Button>(cards[0], "_selectButton");
            Assert.IsTrue(selectButton != null, "RelicCardView._selectButton が null。");

            AssertRaycastReachesButton(selectButton, "relicCard[0]._selectButton");
            bool cardClicked = ExecuteEvents.Execute(
                selectButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(cardClicked, "RelicCardView の SelectButton クリックが受理されなかった。");

            // ドラフトが閉じ、WaitingInput へ復帰することを確認
            yield return WaitForCondition(
                () => !relicDraftDialog.IsVisible && flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"レリックドラフト選択後に入力待ちへ復帰しなかった。IsVisible={relicDraftDialog.IsVisible}, Phase={flow.CurrentPhase}");

            // ---------- ターン 6 のコマンドを実行してターン 7 へ進める ----------
            bool turn6Clicked = ExecuteEvents.Execute(
                studyButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(turn6Clicked, "ターン 6 でのコマンドクリックが受理されなかった。");

            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn == 7,
                () => $"ターン 7 へ進まなかった。CurrentTurn={flow.CurrentState?.CurrentTurn}, Phase={flow.CurrentPhase}");

            yield return WaitForCondition(
                () => flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"ターン 7 開始後に入力待ちへ戻らなかった。Phase={flow.CurrentPhase}");

            // ---------- Assert: 例外 0 件とターン 7 到達 ----------
            Assert.AreEqual(7, flow.CurrentState.CurrentTurn, "ターンが 7 になっていない。");
            Assert.IsEmpty(
                _capturedFailures,
                "ターン 1〜7 進行中に Error / Exception / Assert ログが発生した:\n"
                + string.Join("\n", _capturedFailures));
        }

        [UnityTest]
        public IEnumerator MainGame_AdvanceToTurn24_AllBossesDefeated_ShowsEndingPanel_WithZeroExceptions()
        {
            // ---------- Arrange: MainGame をロード ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"シーン '{SceneName}' のロードを開始できなかった。");
            while (!load.isDone) yield return null;
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            BossBattleDialogView bossDialog = UnityEngine.Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(bossDialog != null, "BossBattleDialogView が MainGame シーンに存在しない。");

            RelicDraftDialogView relicDraftDialog = UnityEngine.Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(relicDraftDialog != null, "RelicDraftDialogView が MainGame シーンに存在しない。");

            EndingView endingView = UnityEngine.Object.FindFirstObjectByType<EndingView>(FindObjectsInactive.Include);
            Assert.IsTrue(endingView != null, "EndingView が MainGame シーンに存在しない。");

            Button studyButton = FindCommandButtonByName(StudyButtonName).GetComponent<Button>();
            Button trainButton = FindCommandButtonByName("TrainButton").GetComponent<Button>();
            Button restButton = FindCommandButtonByName("RestButton").GetComponent<Button>();

            Button bossDismissButton = GetSerializedField<Button>(bossDialog, "_dismissButton");
            List<RelicCardView> relicCards = GetField<List<RelicCardView>>(relicDraftDialog, "_cardViews");

            // 初期状態では EndingPanel と RelicDraftDialogPanel は非アクティブ
            Assert.IsFalse(endingView.IsPanelActive, "初期状態で EndingView がアクティブになっている。");
            Assert.IsFalse(relicDraftDialog.IsVisible, "初期状態で RelicDraftDialogView がアクティブになっている。");

            // ---------- Act: ターン 1 から 24 まで進行 ----------
            float maxTime = Time.realtimeSinceStartup + 30f;
            int bossCount = 0;
            int draftCount = 0;

            while (!endingView.IsPanelActive && flow.CurrentPhase != GamePhase.GameOver)
            {
                if (Time.realtimeSinceStartup > maxTime)
                {
                    Assert.Fail($"30 秒以内にエンディング画面へ到達しなかった。Phase={flow.CurrentPhase}, Turn={flow.CurrentState?.CurrentTurn}");
                }

                if (bossDialog.gameObject.activeInHierarchy && bossDialog.IsVisible)
                {
                    bossCount++;
                    AssertRaycastReachesButton(bossDismissButton, "bossDismissButton");
                    bool dismissed = ExecuteEvents.Execute(
                        bossDismissButton.gameObject,
                        new PointerEventData(EventSystem.current),
                        ExecuteEvents.pointerClickHandler);
                    Assert.IsTrue(dismissed, $"BossBattleDialogView の dismissButton クリックが受理されなかった。bossCount={bossCount}, btnActive={bossDismissButton.gameObject.activeInHierarchy}, btnEnabled={bossDismissButton.isActiveAndEnabled}");
                }
                else if (relicDraftDialog.IsVisible)
                {
                    draftCount++;
                    Assert.IsTrue(relicCards.Count > 0, "RelicDraftDialogView._cardViews が空。");
                    Button cardBtn = GetSerializedField<Button>(relicCards[0], "_selectButton");
                    AssertRaycastReachesButton(cardBtn, "relicCard[0]._selectButton");
                    bool cardClicked = ExecuteEvents.Execute(
                        cardBtn.gameObject,
                        new PointerEventData(EventSystem.current),
                        ExecuteEvents.pointerClickHandler);
                    Assert.IsTrue(cardClicked, "RelicCardView の SelectButton クリックが受理されなかった。");
                }
                else if (flow.CurrentPhase == GamePhase.WaitingInput)
                {
                    int turn = flow.CurrentState.CurrentTurn;
                    bool beforeBoss = (turn == 5 || turn == 11 || turn == 17 || turn == 23);

                    Button chosen;
                    if (beforeBoss && flow.CurrentState.Stamina < 70)
                    {
                        chosen = restButton;
                    }
                    else if (flow.CurrentState.Stamina <= 50)
                    {
                        chosen = restButton;
                    }
                    else if (flow.CurrentState.Mental <= 35)
                    {
                        chosen = studyButton;
                    }
                    else
                    {
                        chosen = trainButton;
                    }

                    bool clicked = ExecuteEvents.Execute(
                        chosen.gameObject,
                        new PointerEventData(EventSystem.current),
                        ExecuteEvents.pointerClickHandler);
                    Assert.IsTrue(clicked, $"コマンドボタン '{chosen.gameObject.name}' のクリックが受理されなかった。");
                }

                yield return null;
            }

            // UI 反映を確定させるため 1 フレーム待つ
            yield return null;

            // ---------- Assert: ターン 24 最終ボス撃破後のエンディング画面表示と例外ゼロ ----------
            Assert.AreEqual(GamePhase.GameClear, flow.CurrentPhase, "GamePhase が GameClear に到達していない。");
            Assert.AreEqual(4, bossCount, "4 回のボス戦ダイアログが表示されていない。");
            Assert.AreEqual(3, draftCount, "Act 1〜3 の 3 回のレリックドラフトが表示されていない。");

            Assert.IsTrue(endingView.IsPanelActive, "ゲームクリア後に EndingView.IsPanelActive が true になっていない。");
            Assert.IsFalse(string.IsNullOrEmpty(endingView.DisplayedResult), "EndingView.DisplayedResult が空文字列。");

            Assert.IsEmpty(
                _capturedFailures,
                "24 ターン進行中に Error / Exception / Assert ログが発生した:\n"
                + string.Join("\n", _capturedFailures));
        }


        /// <summary>
        /// predicate が真になるまで毎フレーム待つ。TimeoutSeconds を超えたら失敗する。
        /// </summary>
        private static IEnumerator WaitForCondition(Func<bool> predicate, Func<string> timeoutMessage)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;

            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(timeoutMessage());
                }

                yield return null;
            }
        }

        /// <summary>
        /// GameObject 名で CommandButtonView を引く。テストコードでのシーン検索は
        /// 00_rules.md の禁止対象（ランタイムコード）ではない。
        /// </summary>
        private static CommandButtonView FindCommandButtonByName(string gameObjectName)
        {
            CommandButtonView[] views = UnityEngine.Object.FindObjectsByType<CommandButtonView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (CommandButtonView view in views)
            {
                if (view.gameObject.name == gameObjectName)
                {
                    return view;
                }
            }

            string found = views.Length == 0
                ? "(なし)"
                : string.Join(", ", Array.ConvertAll(views, v => v.gameObject.name));
            Assert.Fail(
                $"CommandButtonView を持つ GameObject '{gameObjectName}' がシーンに見つからない。検出できたのは: {found}");
            return null;
        }

        /// <summary>
        /// [SerializeField] private フィールドの実体を読む。人間の Inspector アサインが
        /// 落ちている場合に、原因を名指しで失敗させるために使う。
        /// </summary>
        private static T GetSerializedField<T>(Component target, string fieldName) where T : UnityEngine.Object
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(
                field != null, $"{target.GetType().Name}.{fieldName} というフィールドが存在しない。");

            T value = field.GetValue(target) as T;
            UnityEngine.Object asObject = value;
            Assert.IsTrue(
                asObject != null,
                $"{target.GetType().Name}.{fieldName} が Inspector で未アサイン、または型が {typeof(T).Name} でない。"
                + " 人間のアサインを確認せよ。");

            return value;
        }

        /// <summary>
        /// [SerializeField] private フィールド（非 UnityEngine.Object 型）の実体を読む。
        /// </summary>
        private static T GetField<T>(Component target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(
                field != null, $"{target.GetType().Name}.{fieldName} というフィールドが存在しない。");

            object value = field.GetValue(target);
            Assert.IsTrue(
                value != null,
                $"{target.GetType().Name}.{fieldName} が null。");

            return (T)value;
        }

        /// <summary>
        /// GraphicRaycaster を通して指定ボタン（またはその子要素）がクリック可能位置の最前面にあるかを検証する。
        /// 他のモーダルパネル（透明背景など）が上に被さっている場合、検知してテストを失敗させる。
        /// </summary>
        private static void AssertRaycastReachesButton(Button button, string buttonName)
        {
            Assert.IsTrue(button != null, $"{buttonName} is null.");
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"{buttonName} is not active in hierarchy.");

            Canvas canvas = button.GetComponentInParent<Canvas>();
            Assert.IsTrue(canvas != null, $"Canvas not found for {buttonName}.");
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            Assert.IsTrue(raycaster != null, $"GraphicRaycaster not found on Canvas for {buttonName}.");

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, button.transform.position);

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPoint
            };

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            Assert.IsTrue(results.Count > 0, $"Raycast hit nothing at {buttonName} screen position {screenPoint}.");

            GameObject topHit = results[0].gameObject;
            bool isTargetOrChild = topHit == button.gameObject || topHit.transform.IsChildOf(button.transform);
            Assert.IsTrue(
                isTargetOrChild,
                $"Raycast to '{buttonName}' was blocked by '{topHit.name}' (Parent: {topHit.transform.parent?.name}). Target button: {button.gameObject.name}");
        }
    }
}
