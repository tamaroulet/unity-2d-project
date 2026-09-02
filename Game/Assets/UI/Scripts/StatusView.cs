// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// GameStateEventChannelSO を購読し、Stamina, Skill, Mental, Turn の
    /// テキスト・ゲージを更新する薄いビュー。自身はパラメータの計算を行わない。
    /// </summary>
    public class StatusView : MonoBehaviour
    {
        [SerializeField] private GameStateEventChannelSO _gameStateChannel;
        [SerializeField] private TextMeshProUGUI _turnText;
        [SerializeField] private TextMeshProUGUI _staminaText;
        [SerializeField] private TextMeshProUGUI _skillText;
        [SerializeField] private TextMeshProUGUI _mentalText;
        [SerializeField] private Slider _staminaGauge;
        [SerializeField] private Slider _skillGauge;
        [SerializeField] private Slider _mentalGauge;
        [SerializeField] private RectTransform _staminaBarFill;
        [SerializeField] private RectTransform _skillBarFill;
        [SerializeField] private RectTransform _mentalBarFill;

        /// <summary>
        /// 直近に受信した GameState。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public GameState LastDisplayedState { get; private set; }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            if (_gameStateChannel != null)
            {
                _gameStateChannel.OnEventRaised += OnGameStateChanged;
            }
        }

        private void EnsureReferences()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
            if (_gameStateChannel == null)
            {
                _gameStateChannel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameStateEventChannelSO>("Assets/Data/Channels/GameStateEventChannel.asset");
            }
#endif
            if (_turnText == null) _turnText = transform.Find("TurnText")?.GetComponent<TextMeshProUGUI>();
            if (_staminaText == null) _staminaText = transform.Find("StaminaGroup/Label")?.GetComponent<TextMeshProUGUI>();
            if (_skillText == null) _skillText = transform.Find("SkillGroup/Label")?.GetComponent<TextMeshProUGUI>();
            if (_mentalText == null) _mentalText = transform.Find("MentalGroup/Label")?.GetComponent<TextMeshProUGUI>();
            if (_staminaGauge == null) _staminaGauge = transform.Find("StaminaGroup")?.GetComponent<Slider>();
            if (_skillGauge == null) _skillGauge = transform.Find("SkillGroup")?.GetComponent<Slider>();
            if (_mentalGauge == null) _mentalGauge = transform.Find("MentalGroup")?.GetComponent<Slider>();

            if (_staminaBarFill == null) _staminaBarFill = transform.Find("StaminaGroup/BarBg/BarFill")?.GetComponent<RectTransform>();
            if (_skillBarFill == null) _skillBarFill = transform.Find("SkillGroup/BarBg/BarFill")?.GetComponent<RectTransform>();
            if (_mentalBarFill == null) _mentalBarFill = transform.Find("MentalGroup/BarBg/BarFill")?.GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            if (_gameStateChannel != null)
            {
                _gameStateChannel.OnEventRaised -= OnGameStateChanged;
            }
        }

        /// <summary>
        /// Unity EditMode では PlayerLoop が常時動作せず SetActive(true) による
        /// OnEnable() の自動発火が不安定になる場合があるため、テスト等から
        /// チャンネル購読を明示的に行うための公開メソッド。
        /// </summary>
        public void Bind(GameStateEventChannelSO channel)
        {
            _gameStateChannel = channel;
            if (channel != null)
            {
                channel.OnEventRaised += OnGameStateChanged;
            }
        }

        /// <summary>
        /// GameStateEventChannelSO からの通知を受け、テキストとゲージを最新の状態に更新する。
        /// </summary>
        public void OnGameStateChanged(GameState state)
        {
            if (state == null)
            {
                return;
            }

            LastDisplayedState = state;

            if (_turnText != null) _turnText.text = $"TURN {state.CurrentTurn} / 24";
            if (_staminaText != null) _staminaText.text = $"Stamina: {state.Stamina} / 100";
            if (_skillText != null) _skillText.text = $"Skill: {state.Skill}";
            if (_mentalText != null) _mentalText.text = $"Mental: {state.Mental} / 100";

            SetGauge(_staminaGauge, state.Stamina);
            SetGauge(_skillGauge, state.Skill);
            SetGauge(_mentalGauge, state.Mental);

            SetBarFill(_staminaBarFill, (float)state.Stamina / 100f);
            SetBarFill(_skillBarFill, (float)Mathf.Clamp(state.Skill, 0, 100) / 100f);
            SetBarFill(_mentalBarFill, (float)state.Mental / 100f);
        }

        private static void SetGauge(Slider gauge, int value)
        {
            if (gauge != null)
            {
                gauge.value = value;
            }
        }

        private static void SetBarFill(RectTransform barFill, float ratio)
        {
            if (barFill != null)
            {
                float clampedRatio = Mathf.Clamp01(ratio);
                barFill.anchorMin = Vector2.zero;
                barFill.anchorMax = new Vector2(clampedRatio, 1f);
                barFill.sizeDelta = Vector2.zero;
            }
        }
    }
}
