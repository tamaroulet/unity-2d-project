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
    }
}
