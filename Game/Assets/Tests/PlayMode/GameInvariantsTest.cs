// SPDX-AI-Disclosure: ai-generated
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.Relic;
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
    /// 指示書 26: 過去の再発バグを法則で塞ぐ 6 つの不変条件テスト。
    /// UI 操作（ExecuteEvents）経由で実行し、INV-1〜5 の緑および INV-6 の実測値を検証する。
    /// </summary>
    public class GameInvariantsTest
    {
        private const string SceneName = "MainGame";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("Game.MetaProfile");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("Game.MetaProfile");
            PlayerPrefs.Save();
        }

        private static T GetField<T>(Component target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(field != null, $"{target.GetType().Name}.{fieldName} field not found.");
            return (T)field.GetValue(target);
        }

        private static IEnumerator LoadSceneAndInit(System.Action<GameFlowController, Canvas> onReady)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"Failed to load scene '{SceneName}'.");
            while (!load.isDone) yield return null;
            yield return null;

            GameFlowController flow = Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController not found.");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsTrue(canvas != null, "Canvas not found.");

            onReady(flow, canvas);
        }

        [UnityTest]
        public IEnumerator Invariants_FullRun_Clear_INV1_to_INV5_Hold()
        {
            GameFlowController flow = null;
            Canvas canvas = null;
            yield return LoadSceneAndInit((f, c) => { flow = f; canvas = c; });

            BossBattleDialogView bossDialog = Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            RelicDraftDialogView relicDraftDialog = Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            EventDialogView eventDialog = Object.FindFirstObjectByType<EventDialogView>(FindObjectsInactive.Include);
            EndingView endingView = Object.FindFirstObjectByType<EndingView>(FindObjectsInactive.Include);

            Button studyBtn = GameObject.Find("StudyButton")?.GetComponent<Button>();
            Button trainBtn = GameObject.Find("TrainButton")?.GetComponent<Button>();
            Button restBtn = GameObject.Find("RestButton")?.GetComponent<Button>();
            Button bossDismissBtn = GetField<Button>(bossDialog, "_dismissButton");
            Button eventOkBtn = GetField<Button>(eventDialog, "_okButton");
            List<RelicCardView> relicCards = GetField<List<RelicCardView>>(relicDraftDialog, "_cardViews");

            int endingCount = 0;
            EndingDecidedChannelSO endingChannel = GetField<EndingDecidedChannelSO>(flow, "_endingDecidedChannel");
            if (endingChannel != null) endingChannel.OnEventRaised += _ => endingCount++;

            GamePhase lastPhase = flow.CurrentPhase;
            float phaseEnterTime = Time.realtimeSinceStartup;
            string lastAction = "SceneLoaded";

            float deadline = Time.realtimeSinceStartup + 40f;
            while (!endingView.IsPanelActive && Time.realtimeSinceStartup < deadline)
            {
                // INV-1: フェーズ滞留チェック
                GameInvariants.AssertPhaseProgress(flow, ref lastPhase, ref phaseEnterTime, lastAction, timeoutSeconds: 10f);

                // ダイアログ・コマンド処理
                if (bossDialog.gameObject.activeInHierarchy && bossDialog.IsVisible)
                {
                    lastAction = "DismissBossDialog";
                    ExecuteEvents.Execute(bossDismissBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (relicDraftDialog.IsVisible)
                {
                    lastAction = "SelectRelicCard";
                    Button cardBtn = GetField<Button>(relicCards[0], "_selectButton");
                    ExecuteEvents.Execute(cardBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (eventDialog != null && eventDialog.IsPanelActive)
                {
                    lastAction = "DismissEventDialog";
                    ExecuteEvents.Execute(eventOkBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (flow.CurrentPhase == GamePhase.WaitingInput)
                {
                    // INV-3: WaitingInput 中にブロッカーが残っていないこと
                    GameInvariants.AssertNoFullscreenBlockerInWaitingInput(canvas);

                    // INV-4: グリフが揃っていること
                    GameInvariants.AssertAllTextGlyphsPresent();

                    // INV-5: 押せるボタンは押せること
                    GameInvariants.AssertInteractableButtonsReachable();

                    // コマンド選択
                    int turn = flow.CurrentState.CurrentTurn;
                    bool beforeBoss = (turn == 5 || turn == 11 || turn == 17 || turn == 23);
                    Button chosen = (beforeBoss && flow.CurrentState.Stamina < 70) ? restBtn
                        : (flow.CurrentState.Stamina <= 50) ? restBtn
                        : (flow.CurrentState.Mental <= 35) ? studyBtn
                        : trainBtn;

                    lastAction = $"ClickCommand_{chosen.gameObject.name}";
                    ExecuteEvents.Execute(chosen.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }

                yield return null;
            }

            yield return null;

            // INV-2: 終端到達時にエンディングが通知されたこと
            Assert.AreEqual(GamePhase.GameClear, flow.CurrentPhase, "Game did not end in GameClear.");
            GameInvariants.AssertEndingDecided(endingCount, flow.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator Invariants_DefeatRun_INV1_and_INV2_Hold()
        {
            GameFlowController flow = null;
            Canvas canvas = null;
            yield return LoadSceneAndInit((f, c) => { flow = f; canvas = c; });

            BossBattleDialogView bossDialog = Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            EndingView endingView = Object.FindFirstObjectByType<EndingView>(FindObjectsInactive.Include);
            Button trainBtn = GameObject.Find("TrainButton")?.GetComponent<Button>();
            Button bossDismissBtn = GetField<Button>(bossDialog, "_dismissButton");

            int endingCount = 0;
            EndingDecidedChannelSO endingChannel = GetField<EndingDecidedChannelSO>(flow, "_endingDecidedChannel");
            if (endingChannel != null) endingChannel.OnEventRaised += _ => endingCount++;

            GamePhase lastPhase = flow.CurrentPhase;
            float phaseEnterTime = Time.realtimeSinceStartup;
            string lastAction = "SceneLoaded";

            float deadline = Time.realtimeSinceStartup + 30f;
            while (!endingView.IsPanelActive && Time.realtimeSinceStartup < deadline)
            {
                GameInvariants.AssertPhaseProgress(flow, ref lastPhase, ref phaseEnterTime, lastAction, timeoutSeconds: 10f);

                if (bossDialog.gameObject.activeInHierarchy && bossDialog.IsVisible)
                {
                    lastAction = "DismissBossDialog";
                    ExecuteEvents.Execute(bossDismissBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (flow.CurrentPhase == GamePhase.WaitingInput)
                {
                    // Train を連打してスタミナ枯渇状態でボス（ターン6）に突入させる
                    lastAction = "ClickTrainButton";
                    ExecuteEvents.Execute(trainBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }

                yield return null;
            }

            yield return null;

            Assert.AreEqual(GamePhase.GameOver, flow.CurrentPhase, "Game did not end in GameOver.");
            GameInvariants.AssertEndingDecided(endingCount, flow.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator Invariants_INV6_SubsystemsFiredAtLeastOnce()
        {
            GameFlowController flow = null;
            Canvas canvas = null;
            yield return LoadSceneAndInit((f, c) => { flow = f; canvas = c; });

            BossBattleDialogView bossDialog = Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            RelicDraftDialogView relicDraftDialog = Object.FindFirstObjectByType<RelicDraftDialogView>(FindObjectsInactive.Include);
            EventDialogView eventDialog = Object.FindFirstObjectByType<EventDialogView>(FindObjectsInactive.Include);
            EndingView endingView = Object.FindFirstObjectByType<EndingView>(FindObjectsInactive.Include);

            Button studyBtn = GameObject.Find("StudyButton")?.GetComponent<Button>();
            Button restBtn = GameObject.Find("RestButton")?.GetComponent<Button>();
            Button trainBtn = GameObject.Find("TrainButton")?.GetComponent<Button>();
            Button bossDismissBtn = GetField<Button>(bossDialog, "_dismissButton");
            Button eventOkBtn = GetField<Button>(eventDialog, "_okButton");
            List<RelicCardView> relicCards = GetField<List<RelicCardView>>(relicDraftDialog, "_cardViews");

            int eventCount = 0;
            int bossCount = 0;
            int draftCount = 0;

            GameEventFiredChannelSO eventChannel = GetField<GameEventFiredChannelSO>(flow, "_eventFiredChannel");
            if (eventChannel != null) eventChannel.OnEventRaised += _ => eventCount++;
            flow.OnBossBattleOccurred += (_, _, _) => bossCount++;
            flow.OnRelicDraftRequested += _ => draftCount++;

            float deadline = Time.realtimeSinceStartup + 40f;
            while (!endingView.IsPanelActive && flow.CurrentPhase != GamePhase.GameClear && flow.CurrentPhase != GamePhase.GameOver)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Run timeout during INV-6 check.");
                }

                if (bossDialog.gameObject.activeInHierarchy && bossDialog.IsVisible)
                {
                    ExecuteEvents.Execute(bossDismissBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (relicDraftDialog.IsVisible)
                {
                    Button cardBtn = GetField<Button>(relicCards[0], "_selectButton");
                    ExecuteEvents.Execute(cardBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (eventDialog != null && eventDialog.IsPanelActive)
                {
                    ExecuteEvents.Execute(eventOkBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
                else if (flow.CurrentPhase == GamePhase.WaitingInput)
                {
                    int turn = flow.CurrentState.CurrentTurn;
                    bool beforeBoss = (turn == 5 || turn == 11 || turn == 17 || turn == 23);
                    Button chosen = (beforeBoss && flow.CurrentState.Stamina < 70) ? restBtn
                        : (flow.CurrentState.Stamina <= 50) ? restBtn
                        : (flow.CurrentState.Mental <= 35) ? studyBtn
                        : trainBtn;

                    ExecuteEvents.Execute(chosen.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }

                yield return null;
            }

            Debug.Log($"[INV-6 実測値] EventCount={eventCount}, BossCount={bossCount}, DraftCount={draftCount}");

            // INV-6 のアサーション（指示書 26: イベント未発火により現時点では赤になることが期待される）
            GameInvariants.AssertSubsystemsFired(eventCount, bossCount, draftCount);
        }
    }
}
