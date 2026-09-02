// SPDX-AI-Disclosure: ai-generated
using System;
using Game.Features.Relic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// レリックドラフト画面における、1枚のレリックカード表示と選択ボタン。
    /// </summary>
    public class RelicCardView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Button _selectButton;

        private RelicSO _relic;
        private Action<int> _onSelected;

        public RelicSO BoundRelic => _relic;

        public string DisplayedName => _nameText != null ? _nameText.text : string.Empty;

        public string DisplayedDescription => _descriptionText != null ? _descriptionText.text : string.Empty;

        private void OnEnable()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// レリックデータと選択コールバックをバインドする。
        /// </summary>
        public void Bind(RelicSO relic, Action<int> onSelected)
        {
            _relic = relic;
            _onSelected = onSelected;

            if (_relic != null)
            {
                if (_nameText != null) _nameText.text = _relic.DisplayName;
                if (_descriptionText != null) _descriptionText.text = _relic.Description;
            }
        }

        /// <summary>
        /// 選択ボタン押下時のハンドラ。テスト等から直接呼び出し可能。
        /// </summary>
        public void OnButtonClicked()
        {
            if (_relic != null)
            {
                _onSelected?.Invoke(_relic.RelicId);
            }
        }
    }
}
