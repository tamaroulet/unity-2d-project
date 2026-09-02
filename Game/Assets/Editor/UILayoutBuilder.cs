// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using Game.Features.GameFlow;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorScripts
{
    public static class UILayoutBuilder
    {
        private static readonly Color ColorBgHeader = new Color(0.11f, 0.13f, 0.18f, 0.95f);
        private static readonly Color ColorBgFooter = new Color(0.09f, 0.10f, 0.14f, 0.95f);
        private static readonly Color ColorBgDialog = new Color(0.13f, 0.15f, 0.20f, 0.98f);
        private static readonly Color ColorBgBossDialog = new Color(0.18f, 0.11f, 0.13f, 0.98f);
        private static readonly Color ColorCardBg = new Color(0.18f, 0.21f, 0.28f, 1.0f);
        private static readonly Color ColorBorder = new Color(0.28f, 0.32f, 0.42f, 1.0f);
        private static readonly Color ColorOverlay = new Color(0.0f, 0.0f, 0.0f, 0.75f);

        private static readonly Color ColorStamina = new Color(0.22f, 0.75f, 0.45f, 1.0f);
        private static readonly Color ColorSkill = new Color(0.25f, 0.55f, 0.95f, 1.0f);
        private static readonly Color ColorMental = new Color(0.85f, 0.35f, 0.65f, 1.0f);
        private static readonly Color ColorBossHp = new Color(0.90f, 0.25f, 0.25f, 1.0f);
        private static readonly Color ColorShield = new Color(0.25f, 0.75f, 0.95f, 1.0f);

        [MenuItem("Tools/Setup Complete UI Layout (Simple Shapes)")]
        public static void SetupCompleteLayout()
        {
            string scenePath = "Assets/Scenes/MainGame.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Canvas Scaler 設定 (1920 x 1080 基準)
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 1. StatusPanel のレイアウト
            SetupStatusPanel(canvas.transform);

            // 2. CommandButtonsPanel のレイアウト
            SetupCommandPanel(canvas.transform);

            // 3. 各ダイアログ（単純図形ワイヤーフレーム）
            SetupEventDialogPanel(canvas.transform);
            SetupRelicDraftDialogPanel(canvas.transform);
            SetupBossBattleDialogPanel(canvas.transform);
            SetupMetaShopDialogPanel(canvas.transform);
            SetupEndingPanel(canvas.transform);

            // 4. GameFlowController の参照再バインド
            RebindGameFlowControllerReferences(canvas.transform);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[UILayoutBuilder] Complete UI Layout (Simple Shapes) configured and saved successfully.");
        }

        private static void SetupStatusPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("StatusPanel");
            if (tr == null) return;
            GameObject go = tr.gameObject;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.80f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(20f, -10f);
            rect.offsetMax = new Vector2(-20f, -10f);

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = ColorBgHeader;
        }

        private static void SetupCommandPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("CommandButtonsPanel");
            if (tr == null) return;
            GameObject go = tr.gameObject;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.22f);
            rect.offsetMin = new Vector2(20f, 15f);
            rect.offsetMax = new Vector2(-20f, 15f);

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = ColorBgFooter;
        }

        private static void SetupEventDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("EventDialogPanel");
            if (tr == null) return;
            ConfigureModalPanel(tr, new Vector2(800, 500), ColorBgDialog);
        }

        private static void SetupRelicDraftDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("RelicDraftDialogPanel");
            if (tr == null) return;
            ConfigureModalPanel(tr, new Vector2(1100, 600), ColorBgDialog);
        }

        private static void SetupBossBattleDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("BossBattleDialogPanel");
            if (tr == null) return;
            ConfigureModalPanel(tr, new Vector2(1200, 700), ColorBgBossDialog);
        }

        private static void SetupMetaShopDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("MetaShopDialogPanel");
            if (tr == null) return;
            ConfigureModalPanel(tr, new Vector2(1100, 650), ColorBgDialog);
        }

        private static void SetupEndingPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("EndingPanel");
            if (tr == null) return;
            ConfigureModalPanel(tr, new Vector2(900, 600), ColorBgDialog);
        }

        private static void ConfigureModalPanel(Transform panelTr, Vector2 size, Color bgColor)
        {
            RectTransform rect = panelTr.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 背景暗転オーバーレイ
            Image overlayImg = panelTr.GetComponent<Image>() ?? panelTr.gameObject.AddComponent<Image>();
            overlayImg.color = ColorOverlay;

            Transform rootTr = panelTr.Find("PanelRoot");
            if (rootTr != null)
            {
                RectTransform rootRect = rootTr.GetComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.sizeDelta = size;
                rootRect.anchoredPosition = Vector2.zero;

                Image rootImg = rootTr.GetComponent<Image>() ?? rootTr.gameObject.AddComponent<Image>();
                rootImg.color = bgColor;
            }
        }

        private static void RebindGameFlowControllerReferences(Transform canvasTr)
        {
            GameFlowController controller = Object.FindFirstObjectByType<GameFlowController>();
            if (controller == null) return;

            SerializedObject so = new SerializedObject(controller);
            SetViewProperty(so, "_statusView", canvasTr.Find("StatusPanel"));
            SetViewProperty(so, "_commandButtonsView", canvasTr.Find("CommandButtonsPanel"));
            SetViewProperty(so, "_eventDialogView", canvasTr.Find("EventDialogPanel"));
            SetViewProperty(so, "_endingView", canvasTr.Find("EndingPanel"));
            SetViewProperty(so, "_relicDraftDialogView", canvasTr.Find("RelicDraftDialogPanel"));
            SetViewProperty(so, "_bossBattleDialogView", canvasTr.Find("BossBattleDialogPanel"));
            SetViewProperty(so, "_metaShopDialogView", canvasTr.Find("MetaShopDialogPanel"));
            so.ApplyModifiedProperties();
        }

        private static void SetViewProperty(SerializedObject so, string propName, Transform tr)
        {
            if (tr == null) return;
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                Component view = tr.GetComponent(propName.TrimStart('_').Replace("View", "View").Replace("ButtonsView", "ButtonsView"));
                if (view == null)
                {
                    // コンポーネント型に応じた取得
                    view = tr.GetComponent<MonoBehaviour>();
                }
                if (view != null)
                {
                    prop.objectReferenceValue = view;
                }
            }
        }
    }
}
#endif
