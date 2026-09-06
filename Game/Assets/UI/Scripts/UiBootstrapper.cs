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
        private static readonly Color ColorBgHeader = new Color(0.12f, 0.15f, 0.20f, 0.98f);
        private static readonly Color ColorBarBg = new Color(0.18f, 0.20f, 0.26f, 1.0f);
        private static readonly Color ColorStamina = new Color(0.22f, 0.85f, 0.45f, 1.0f);
        private static readonly Color ColorSkill = new Color(0.25f, 0.65f, 0.98f, 1.0f);
        private static readonly Color ColorMental = new Color(0.92f, 0.35f, 0.65f, 1.0f);

        [SerializeField] private Canvas _canvas;
        [SerializeField] private EndingView _endingView;
        [SerializeField] private GameFlowController _gameFlowController;

        private void Awake()
        {
            BootstrapStatusPanel();
            BootstrapEndingPanel();
        }

        private void BootstrapStatusPanel()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            }

            GameStateEventChannelSO gameStateChannel = Resources.Load<GameStateEventChannelSO>("GameStateChannel");
            if (gameStateChannel == null)
            {
                Debug.LogError("[UiBootstrapper] Failed to load GameStateEventChannelSO from Resources.");
            }

            // StatusPanel (ヘッダー枠)
            GameObject panelGo = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image), typeof(StatusView));
            panelGo.transform.SetParent(_canvas.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.82f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(20f, -10f);
            panelRect.offsetMax = new Vector2(-20f, -10f);

            Image panelImg = panelGo.GetComponent<Image>();
            panelImg.color = ColorBgHeader;

            StatusView statusView = panelGo.GetComponent<StatusView>();

            // ゲージ3本
            CreateGaugeGroup(
                panelRect, "StaminaGroup", ColorStamina, new Vector2(-400, 0), "Stamina: 50 / 100",
                out Slider staminaGauge, out RectTransform staminaBarFill, out TextMeshProUGUI staminaText);

            CreateGaugeGroup(
                panelRect, "SkillGroup", ColorSkill, new Vector2(0, 0), "Skill: 10",
                out Slider skillGauge, out RectTransform skillBarFill, out TextMeshProUGUI skillText);

            CreateGaugeGroup(
                panelRect, "MentalGroup", ColorMental, new Vector2(400, 0), "Mental: 80 / 100",
                out Slider mentalGauge, out RectTransform mentalBarFill, out TextMeshProUGUI mentalText);

            // テキストラベル
            TextMeshProUGUI turnText = CreateLabel(
                panelRect, "TurnText", "TURN 1 / 24", new Vector2(-750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Left);

            TextMeshProUGUI pointsText = CreateLabel(
                panelRect, "PointsText", "POINTS: 0", new Vector2(750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Right);

            BossBattleDialogView bossBattleDialog = _canvas.GetComponentInChildren<BossBattleDialogView>(true);

            // StatusView への直接代入（Bind）
            statusView.Bind(
                gameStateChannel,
                _gameFlowController,
                bossBattleDialog,
                turnText,
                staminaText,
                skillText,
                mentalText,
                pointsText,
                staminaGauge,
                skillGauge,
                mentalGauge,
                staminaBarFill,
                skillBarFill,
                mentalBarFill);
        }

        private static void CreateGaugeGroup(
            RectTransform parent,
            string name,
            Color barColor,
            Vector2 pos,
            string label,
            out Slider slider,
            out RectTransform barFillRect,
            out TextMeshProUGUI labelTmp)
        {
            GameObject groupGo = new GameObject(name, typeof(RectTransform));
            groupGo.transform.SetParent(parent, false);

            RectTransform groupRect = groupGo.GetComponent<RectTransform>();
            groupRect.anchoredPosition = pos;
            groupRect.sizeDelta = new Vector2(320, 60);

            // BarBg (Slider コンポーネントを担う)
            GameObject barBgGo = new GameObject("BarBg", typeof(RectTransform), typeof(Image), typeof(Slider));
            barBgGo.transform.SetParent(groupRect, false);

            RectTransform barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(20, -5);
            barBgRect.sizeDelta = new Vector2(220, 24);

            Image barBgImg = barBgGo.GetComponent<Image>();
            barBgImg.color = ColorBarBg;

            // BarFill
            GameObject barFillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFillGo.transform.SetParent(barBgRect, false);

            barFillRect = barFillGo.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0f, 1f);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            barFillRect.sizeDelta = Vector2.zero;

            Image barFillImg = barFillGo.GetComponent<Image>();
            barFillImg.color = barColor;

            // Slider 設定（表示専用・入力不可）
            slider = barBgGo.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.handleRect = null;
            slider.targetGraphic = null;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = false;
            slider.fillRect = barFillRect;

            // Label
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(groupRect, false);

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(20, 15);
            labelRect.sizeDelta = new Vector2(220, 24);

            labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = Color.white;
            labelTmp.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateLabel(
            RectTransform parent,
            string name,
            string text,
            Vector2 pos,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject labelGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            RectTransform rect = labelGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
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
