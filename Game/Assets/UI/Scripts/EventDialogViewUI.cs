// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Event;
using Game.Features.GameFlow;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class EventDialogViewUI : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private GameEventFiredChannelSO _eventFiredChannel;

        private GameEventCatalogSO _eventCatalog;
        private GameFlowController _gameFlowController;

        private VisualElement _dialogRoot;
        private Label _titleLabel;
        private Label _bodyLabel;
        private Button _okButton;

        public bool IsPanelActive => _dialogRoot != null && _dialogRoot.style.display == DisplayStyle.Flex;
        public string DisplayedTitle => _titleLabel != null ? _titleLabel.text : string.Empty;
        public string DisplayedBody => _bodyLabel != null ? _bodyLabel.text : string.Empty;

        private void OnEnable()
        {
            if (!ResolveVisualElements()) return;
            SetPanelActive(false);
            if (_eventFiredChannel != null) _eventFiredChannel.OnEventRaised += OnEventFired;
            if (_okButton != null) _okButton.clicked += OnEventDismissed;
        }

        private void OnDisable()
        {
            if (_eventFiredChannel != null) _eventFiredChannel.OnEventRaised -= OnEventFired;
            if (_okButton != null) _okButton.clicked -= OnEventDismissed;
        }

        public bool ResolveVisualElements()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("[EventDialogViewUI] Missing UIDocument reference.");
                return false;
            }
            if (_uiDocument.rootVisualElement == null)
            {
                Debug.LogError("[EventDialogViewUI] UIDocument.rootVisualElement is null.");
                return false;
            }
            return BindVisualElements(_uiDocument.rootVisualElement);
        }

        public bool BindVisualElements(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[EventDialogViewUI] Root VisualElement is null.");
                return false;
            }
            _titleLabel = root.Q<Label>("TitleLabel");
            if (_titleLabel == null)
            {
                Debug.LogError("[EventDialogViewUI] Failed to resolve required element: 'TitleLabel' (Label)");
                return false;
            }
            _bodyLabel = root.Q<Label>("BodyLabel");
            if (_bodyLabel == null)
            {
                Debug.LogError("[EventDialogViewUI] Failed to resolve required element: 'BodyLabel' (Label)");
                return false;
            }
            _okButton = root.Q<Button>("OkButton");
            if (_okButton == null)
            {
                Debug.LogError("[EventDialogViewUI] Failed to resolve required element: 'OkButton' (Button)");
                return false;
            }
            _dialogRoot = root.Q<VisualElement>("DialogOverlay") ?? root;
            return true;
        }

        public void Bind(GameEventFiredChannelSO channel, GameEventCatalogSO catalog = null, GameFlowController gameFlowController = null)
        {
            _eventFiredChannel = channel;
            _eventCatalog = catalog;
            _gameFlowController = gameFlowController;
            if (channel != null) channel.OnEventRaised += OnEventFired;
        }

        public void OnEventFired(int eventId)
        {
            GameEventSO firedEvent = FindEvent(eventId);
            if (firedEvent != null)
            {
                if (_titleLabel != null) _titleLabel.text = firedEvent.DisplayName;
                if (_bodyLabel != null) _bodyLabel.text = firedEvent.Body;
            }
            else
            {
                if (_titleLabel != null) _titleLabel.text = $"Event #{eventId}";
                if (_bodyLabel != null) _bodyLabel.text = string.Empty;
            }
            SetPanelActive(true);
        }

        private void OnEventDismissed()
        {
            SetPanelActive(false);
            if (_gameFlowController != null && _gameFlowController.CurrentPhase == GamePhase.ShowingEvent)
            {
                _gameFlowController.OnEventDismissed();
            }
        }

        public void Dismiss() => OnEventDismissed();

        private void SetPanelActive(bool active)
        {
            if (_dialogRoot != null)
            {
                _dialogRoot.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private GameEventSO FindEvent(int eventId)
        {
            if (_eventCatalog == null || _eventCatalog.Events == null) return null;
            IReadOnlyList<GameEventSO> events = _eventCatalog.Events;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].EventId == eventId) return events[i];
            }
            return null;
        }
    }
}
