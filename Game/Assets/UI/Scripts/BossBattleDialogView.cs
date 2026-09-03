// SPDX-AI-Disclosure: ai-generated
using System;
using System.Text;
using Game.Features.Boss;
using Game.Features.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// ボスバトルダイアログの表示・ステータスゲージ更新・ログ表示を制御する View コンポーネント。
    /// GameFlowController またはイベントチャンネルからの通知を受けて表示を行う。
    /// </summary>
    public class BossBattleDialogView : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _bossHpText;
        [SerializeField] private Slider _bossHpSlider;
        [SerializeField] private TextMeshProUGUI _shieldText;
        [SerializeField] private TextMeshProUGUI _battleLogText;
        [SerializeField] private Button _dismissButton;
        [SerializeField] private TextMeshProUGUI _dismissButtonText;

        private Action _onDismissedCallback;

        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;
        public bool IsVisible => IsPanelActive;

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (_panelRoot == null) _panelRoot = transform.Find("PanelRoot")?.gameObject ?? gameObject;
            if (_bossNameText == null) _bossNameText = transform.Find("PanelRoot/BossTitleText")?.GetComponent<TextMeshProUGUI>();
            if (_bossHpText == null) _bossHpText = transform.Find("PanelRoot/BossHpGroup/Label")?.GetComponent<TextMeshProUGUI>();
            if (_shieldText == null) _shieldText = transform.Find("PanelRoot/BossShieldGroup/Label")?.GetComponent<TextMeshProUGUI>();
            if (_dismissButton == null) _dismissButton = transform.Find("PanelRoot/AutoBattleNextButton")?.GetComponent<Button>();

            if (_dismissButtonText == null)
            {
                _dismissButtonText = ResolveDismissButtonText();
            }

            if (_dismissButtonText != null)
            {
                _dismissButtonText.text = "AUTO BATTLE / NEXT";
            }
        }

        /// <summary>
        /// 決定ボタンのラベルを解決する。ボス名・HP・シールド・戦闘ログとして
        /// 既にバインドされているテキストは、誤ってボタンラベルとして上書きしないよう除外する。
        /// </summary>
        private TextMeshProUGUI ResolveDismissButtonText()
        {
            if (_dismissButton == null)
            {
                return null;
            }

            foreach (TextMeshProUGUI candidate in _dismissButton.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (candidate == _bossNameText || candidate == _bossHpText ||
                    candidate == _shieldText || candidate == _battleLogText)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private void OnEnable()
        {
            EnsureReferences();
            if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveListener(OnDismissButtonClicked);
                _dismissButton.onClick.AddListener(OnDismissButtonClicked);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            GameFlowController controller = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            if (controller != null && (controller.CurrentPhase == GamePhase.ShowingRelicDraft || controller.CurrentPhase == GamePhase.GameOver) && controller.LastEncounteredBoss != null && !IsPanelActive)
            {
                Show(controller.LastEncounteredBoss, controller.LastBossBattleResult, () =>
                {
                    if (controller.CurrentPhase == GamePhase.ShowingRelicDraft)
                    {
                        controller.OnRelicAcquired(controller.LastEncounteredBoss.BossId);
                    }
                    else if (controller.CurrentPhase == GamePhase.GameOver)
                    {
                        controller.StartGame();
                    }
                });
            }
        }

        private void OnDisable()
        {
            if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveListener(OnDismissButtonClicked);
            }
        }

        /// <summary>
        /// ボス戦の結果とターン履歴を受け取ってダイアログを表示する。
        /// </summary>
        public void Show(BossSO boss, FullBattleResult battleResult, Action onDismissed = null)
        {
            EnsureReferences();
            gameObject.SetActive(true);
            _onDismissedCallback = onDismissed;

            if (_bossNameText != null)
            {
                _bossNameText.text = boss != null ? boss.BossName : "BOSS BATTLE";
            }

            if (_dismissButtonText == null)
            {
                _dismissButtonText = ResolveDismissButtonText();
            }

            if (_dismissButtonText != null)
            {
                _dismissButtonText.text = battleResult != null && battleResult.Outcome == BattleOutcomeKind.Victory
                    ? "VICTORY / NEXT ACT"
                    : "DEFEAT / RESTART";
            }

            if (battleResult != null)
            {
                UpdateBossStatus(battleResult.FinalBossState);
                UpdateBattleLog(battleResult);
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// ボスのHPおよびシールド表示を更新する。
        /// </summary>
        public void UpdateBossStatus(BossState bossState)
        {
            if (bossState == null)
            {
                return;
            }

            if (_bossHpText != null)
            {
                _bossHpText.text = $"HP: {bossState.CurrentHp} / {bossState.MaxHp}";
            }

            if (_bossHpSlider != null)
            {
                _bossHpSlider.maxValue = bossState.MaxHp;
                _bossHpSlider.value = bossState.CurrentHp;
            }

            if (_shieldText != null)
            {
                _shieldText.text = bossState.Shield > 0 ? $"Shield: {bossState.Shield}" : string.Empty;
            }
        }

        /// <summary>
        /// 戦闘ターン履歴から要約ログを構築して表示する。
        /// </summary>
        private void UpdateBattleLog(FullBattleResult battleResult)
        {
            if (_battleLogText == null || battleResult == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(battleResult.Outcome == BattleOutcomeKind.Victory ? "[VICTORY] Boss Defeated!" : "[DEFEAT] Player Defeated...");

            if (battleResult.TurnHistory != null)
            {
                for (int i = 0; i < battleResult.TurnHistory.Count; i++)
                {
                    var turn = battleResult.TurnHistory[i];
                    sb.AppendLine($"Turn {i + 1}: Player -{turn.DamageDealtToBoss} DMG | Boss Action: {turn.BossActionTaken}");
                }
            }

            _battleLogText.text = sb.ToString();
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        public void Dismiss()
        {
            Hide();
            _onDismissedCallback?.Invoke();
            _onDismissedCallback = null;
        }

        private void OnDismissButtonClicked()
        {
            Dismiss();
        }
    }
}
