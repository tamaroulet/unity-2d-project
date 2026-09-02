// SPDX-AI-Disclosure: ai-generated
using System;
using System.Text;
using Game.Features.Boss;
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

        private Action _onDismissedCallback;

        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;
        public bool IsVisible => IsPanelActive;

        private void OnEnable()
        {
            if (_dismissButton != null)
            {
                _dismissButton.onClick.AddListener(OnDismissButtonClicked);
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
            _onDismissedCallback = onDismissed;

            if (_bossNameText != null)
            {
                _bossNameText.text = boss != null ? boss.BossName : "Boss";
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
            sb.AppendLine(battleResult.Outcome == BattleOutcomeKind.Victory ? "[VICTORY] ボス撃破！" : "[DEFEAT] 敗北...");

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
