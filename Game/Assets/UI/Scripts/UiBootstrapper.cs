// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using Game.Features.Ending;
using Game.Features.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI パネルを実行時にコードから動的生成し、View に依存を注入するランタイムブートストラッパー。
    /// ADR 0002 (Code-first Bootstrap) に基づき、シーンへのキャッシュをやめて実行時に構築する。
    /// </summary>
    public class UiBootstrapper : MonoBehaviour
    {
        private static readonly Color ColorBgDialog = new Color(0.14f, 0.17f, 0.24f, 0.98f);
        private static readonly Color ColorOverlay = new Color(0.0f, 0.0f, 0.0f, 0.80f);
        private static readonly Color ColorModalButtonBg = new Color(0.25f, 0.45f, 0.85f, 1.0f);

        [SerializeField] private Canvas _canvas;
        [SerializeField] private EndingView _endingView;
        [SerializeField] private GameFlowController _gameFlowController;

        private void Awake()
        {
            BootstrapEndingPanel();
        }

        private void BootstrapEndingPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            EndingDecidedChannelSO endingChannel = Resources.Load<EndingDecidedChannelSO>("EndingDecidedChannel");
            if (endingChannel == null)
            {
                Debug.LogError("[UiBootstrapper] Failed to load EndingDecidedChannelSO from Resources.");
            }

            // EndingPanel (全画面モーダルオーバーレイ)
            GameObject panelGo = new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(_canvas.transform, false);
            panelGo.transform.SetAsLastSibling();

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image overlayImg = panelGo.GetComponent<Image>();
            overlayImg.color = ColorOverlay;

            // PanelRoot (ダイアログ本体枠)
            GameObject rootGo = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelGo.transform, false);

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(950, 650);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>();
            rootImg.color = ColorBgDialog;

            // EndingTitleText
            GameObject titleGo = new GameObject("EndingTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(rootGo.transform, false);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 220);
            titleRect.sizeDelta = new Vector2(700, 60);
            TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "GAME CLEAR!";
            titleTmp.fontSize = 38;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;

            // EndingDescriptionText
            GameObject descGo = new GameObject("EndingDescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(rootGo.transform, false);
            RectTransform descRect = descGo.GetComponent<RectTransform>();
            descRect.anchoredPosition = new Vector2(0, 60);
            descRect.sizeDelta = new Vector2(700, 150);
            TextMeshProUGUI descTmp = descGo.GetComponent<TextMeshProUGUI>();
            descTmp.text = "You survived all 24 turns and defeated all 4 Act Bosses!\nEarned MetaPoints: +150 Pts";
            descTmp.fontSize = 24;
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.color = Color.white;
            descTmp.raycastTarget = false;

            // RestartButton
            GameObject btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rootGo.transform, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -180);
            btnRect.sizeDelta = new Vector2(450, 70);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = ColorModalButtonBg;

            Button restartButton = btnGo.GetComponent<Button>();

            // RestartButton Text
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchoredPosition = Vector2.zero;
            btnTextRect.sizeDelta = new Vector2(450, 70);
            TextMeshProUGUI btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "RESTART / SHOP";
            btnTmp.fontSize = 22;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            btnTmp.raycastTarget = false;

            panelGo.SetActive(false);

            // EndingView への依存注入
            if (_endingView != null)
            {
                _endingView.Bind(endingChannel, _gameFlowController, restartButton, panelGo, titleTmp);
            }
            else
            {
                Debug.LogError("[UiBootstrapper] _endingView is null, cannot bind EndingPanel references.");
            }
        }
    }
}
