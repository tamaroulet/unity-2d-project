// SPDX-AI-Disclosure: ai-generated
using System;
using Game.Features.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// メタショップ（周回アンロック購入）ダイアログの表示とポイント管理を行う View コンポーネント。
    /// </summary>
    public class MetaShopDialogView : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _availablePointsText;
        [SerializeField] private TextMeshProUGUI _totalRunsText;
        [SerializeField] private Button _closeButton;

        private Action _onCloseCallback;

        public bool IsPanelActive => _panelRoot != null && _panelRoot.activeSelf;

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        public void Show(MetaProfileState profile, Action onClose = null)
        {
            _onCloseCallback = onClose;
            UpdateProfile(profile);

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        public void UpdateProfile(MetaProfileState profile)
        {
            if (profile == null)
            {
                return;
            }

            if (_availablePointsText != null)
            {
                _availablePointsText.text = $"MetaPoints: {profile.AvailableMetaPoints}";
            }

            if (_totalRunsText != null)
            {
                _totalRunsText.text = $"Runs Completed: {profile.TotalRunsCompleted}";
            }
        }

        public void Dismiss()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            _onCloseCallback?.Invoke();
            _onCloseCallback = null;
        }

        private void OnCloseButtonClicked()
        {
            Dismiss();
        }
    }
}
