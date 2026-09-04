// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.GameFlow;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorScripts
{
    public static class UILayoutBuilder
    {
        private static readonly Color ColorBgMain = new Color(0.08f, 0.09f, 0.12f, 1.0f);
        private static readonly Color ColorBgHeader = new Color(0.12f, 0.15f, 0.20f, 0.98f);
        private static readonly Color ColorBgFooter = new Color(0.10f, 0.12f, 0.16f, 0.98f);
        private static readonly Color ColorBgDialog = new Color(0.14f, 0.17f, 0.24f, 0.98f);
        private static readonly Color ColorBgBossDialog = new Color(0.20f, 0.10f, 0.12f, 0.98f);
        private static readonly Color ColorButtonBg = new Color(0.18f, 0.22f, 0.30f, 1.0f);
        private static readonly Color ColorBarBg = new Color(0.18f, 0.20f, 0.26f, 1.0f);
        private static readonly Color ColorOverlay = new Color(0.0f, 0.0f, 0.0f, 0.80f);

        private static readonly Color ColorStamina = new Color(0.22f, 0.85f, 0.45f, 1.0f);
        private static readonly Color ColorSkill = new Color(0.25f, 0.65f, 0.98f, 1.0f);
        private static readonly Color ColorMental = new Color(0.92f, 0.35f, 0.65f, 1.0f);
        private static readonly Color ColorBossHp = new Color(0.92f, 0.25f, 0.25f, 1.0f);
        private static readonly Color ColorShield = new Color(0.30f, 0.75f, 0.95f, 1.0f);

        [MenuItem("Tools/Setup Complete UI Layout (Simple Shapes)")]
        public static void SetupCompleteLayout()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[UILayoutBuilder] PlayMode detected. Stopping PlayMode to rebuild UI safely...");
                EditorApplication.isPlaying = false;
            }

            // 1. スプライト生成とインポートを完了
            ProceduralSpriteGenerator.GenerateAllSprites();
            AssetDatabase.SaveAssets();

            // 2. シーンを開いて新規参照を取得
            string scenePath = "Assets/Scenes/MainGame.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 3. Canvas の取得
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem（新 Input System: InputSystemUIInputModule 対応）
            UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
            else
            {
                // 古い StandaloneInputModule があれば除去して InputSystemUIInputModule に置き換え
                UnityEngine.EventSystems.StandaloneInputModule oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null)
                {
                    Object.DestroyImmediate(oldModule);
                }
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            // 古い壊れた子オブジェクトを一括クリア（クリーンビルド）
            int childCount = canvas.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                Object.DestroyImmediate(child.gameObject);
            }

            // 4. 全画面背景
            CreateOrUpdateBackground(canvas.transform);

            // 5. 各 UI パネルの完全構築
            SetupStatusPanel(canvas.transform);
            SetupCommandPanel(canvas.transform);
            SetupEventDialogPanel(canvas.transform);
            SetupRelicDraftDialogPanel(canvas.transform);
            SetupBossBattleDialogPanel(canvas.transform);
            SetupMetaShopDialogPanel(canvas.transform);
            SetupEndingPanel(canvas.transform);

            // 6. UIViews の構築（常時アクティブな View コンポーネントホスト）
            SetupUIViews(canvas.transform);

            // 7. GameFlowController へのバインド
            RebindGameFlowControllerReferences(canvas.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UILayoutBuilder] Full UI Layout successfully built and saved without errors.");
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/Sprites/{name}.png");
        }

        private static void CreateOrUpdateBackground(Transform canvasTr)
        {
            Transform bgTr = canvasTr.Find("Background");
            GameObject bgGo = bgTr != null ? bgTr.gameObject : new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasTr, false);
            bgGo.transform.SetAsFirstSibling();

            RectTransform rect = bgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = bgGo.GetComponent<Image>();
            img.color = ColorBgMain;
            img.raycastTarget = false;
        }

        private static void SetupStatusPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("StatusPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("StatusPanel", typeof(RectTransform), typeof(Image), typeof(StatusView));
            go.transform.SetParent(canvasTr, false);

            RectTransform rect = EnsureRectTransform(go);
            rect.anchorMin = new Vector2(0f, 0.82f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(20f, -10f);
            rect.offsetMax = new Vector2(-20f, -10f);

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = ColorBgHeader;

            StatusView view = go.GetComponent<StatusView>() ?? go.AddComponent<StatusView>();

            // ゲージとテキストの配置
            GameObject staminaGo = CreateGaugeGroup(rect, "StaminaGroup", "Icon_Stamina", ColorStamina, new Vector2(-400, 0), "Stamina: 50 / 100");
            GameObject skillGo = CreateGaugeGroup(rect, "SkillGroup", "Icon_Skill", ColorSkill, new Vector2(0, 0), "Skill: 10");
            GameObject mentalGo = CreateGaugeGroup(rect, "MentalGroup", "Icon_Mental", ColorMental, new Vector2(400, 0), "Mental: 80 / 100");

            GameObject turnGo = CreateLabel(rect, "TurnText", "TURN 1 / 24", new Vector2(-750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Left);
            GameObject pointsGo = CreateLabel(rect, "PointsText", "POINTS: 0", new Vector2(750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Right);

            // StatusView の SerializedObject バインド
            GameStateEventChannelSO channel = AssetDatabase.LoadAssetAtPath<GameStateEventChannelSO>("Assets/Data/Channels/GameStateChannel.asset");
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_gameStateChannel").objectReferenceValue = channel;
            so.FindProperty("_turnText").objectReferenceValue = turnGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_metaPointsText").objectReferenceValue = pointsGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_staminaText").objectReferenceValue = staminaGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_skillText").objectReferenceValue = skillGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_mentalText").objectReferenceValue = mentalGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_staminaBarFill").objectReferenceValue = staminaGo.transform.Find("BarBg/BarFill")?.GetComponent<RectTransform>();
            so.FindProperty("_skillBarFill").objectReferenceValue = skillGo.transform.Find("BarBg/BarFill")?.GetComponent<RectTransform>();
            so.FindProperty("_mentalBarFill").objectReferenceValue = mentalGo.transform.Find("BarBg/BarFill")?.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();

        }

        private static void SetupCommandPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("CommandPanel") ?? canvasTr.Find("CommandButtonsPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("CommandPanel", typeof(RectTransform), typeof(Image));
            go.name = "CommandPanel";
            go.transform.SetParent(canvasTr, false);

            RectTransform rect = EnsureRectTransform(go);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.22f);
            rect.offsetMin = new Vector2(20f, 15f);
            rect.offsetMax = new Vector2(-20f, 15f);

            Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = ColorBgFooter;

            GameFlowController flowController = Object.FindFirstObjectByType<GameFlowController>();

            // 3つのコマンドボタン（STUDY, TRAIN, REST）を完全配線
            SetupCommandButton(rect, "StudyButton", "Icon_Study", "STUDY\nSkill+5", new Vector2(-400, 0), "Assets/Data/Commands/Study.asset", flowController);
            SetupCommandButton(rect, "TrainButton", "Icon_Train", "TRAIN\nSkill+10", new Vector2(0, 0), "Assets/Data/Commands/Train.asset", flowController);
            SetupCommandButton(rect, "RestButton", "Icon_Rest", "REST\nStamina+30", new Vector2(400, 0), "Assets/Data/Commands/Rest.asset", flowController);
        }

        private static void SetupCommandButton(RectTransform parent, string name, string iconName, string text, Vector2 pos, string cmdAssetPath, GameFlowController controller)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CommandButtonView));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(btnGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 120);

            Image img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = ColorButtonBg;

            AttachIcon(rect, "Icon", LoadSprite(iconName), new Vector2(-80, 0), new Vector2(56, 56));
            GameObject labelGo = CreateLabel(rect, "Text", text, new Vector2(40, 0), new Vector2(180, 80), 22, TextAlignmentOptions.Center);

            CommandDataSO cmd = AssetDatabase.LoadAssetAtPath<CommandDataSO>(cmdAssetPath);
            Button btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
            CommandButtonView btnView = btnGo.GetComponent<CommandButtonView>() ?? btnGo.AddComponent<CommandButtonView>();

            SerializedObject so = new SerializedObject(btnView);
            so.FindProperty("_command").objectReferenceValue = cmd;
            so.FindProperty("_gameFlowController").objectReferenceValue = controller;
            so.FindProperty("_button").objectReferenceValue = btn;
            so.FindProperty("_nameText").objectReferenceValue = labelGo.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        private static void SetupEventDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("EventDialogPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("EventDialogPanel", typeof(RectTransform), typeof(Image), typeof(EventDialogView));
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(850, 520), ColorBgDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EventTitleText", "EVENT OCCURRED", new Vector2(0, 180), new Vector2(700, 50), 32, TextAlignmentOptions.Center);
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EventDescriptionText", "A training event has occurred.\nChoose your option carefully.", new Vector2(0, 50), new Vector2(700, 120), 22, TextAlignmentOptions.Center);
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "OptionAButton", "Option A (Stamina Cost / Skill Boost)", new Vector2(0, -90), new Vector2(600, 60));
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "OptionBButton", "Option B (Safe Action / Mental Guard)", new Vector2(0, -170), new Vector2(600, 60));
            }
            go.SetActive(false); // 初期状態は非表示
        }

        private static void SetupRelicDraftDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("RelicDraftDialogPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("RelicDraftDialogPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(1150, 650), ColorBgDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "DraftTitleText", "RELIC DRAFT (SELECT PASSIVE)", new Vector2(0, 240), new Vector2(800, 50), 32, TextAlignmentOptions.Center);
                CreateRelicCard(rootTr.GetComponent<RectTransform>(), "Card1", "Iron Dumbbell", "+5 Stamina/Turn", new Vector2(-340, -20));
                CreateRelicCard(rootTr.GetComponent<RectTransform>(), "Card2", "Book of Wisdom", "+20% Skill Gain", new Vector2(0, -20));
                CreateRelicCard(rootTr.GetComponent<RectTransform>(), "Card3", "Healing Amulet", "+30% Mental Guard", new Vector2(340, -20));
            }

            go.SetActive(false);
        }

        private static void SetupBossBattleDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("BossBattleDialogPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("BossBattleDialogPanel", typeof(RectTransform), typeof(Image));
            BossBattleDialogView oldView = go.GetComponent<BossBattleDialogView>();
            if (oldView != null)
            {
                Object.DestroyImmediate(oldView);
            }
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(1250, 750), ColorBgBossDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "BossTitleText", "BOSS BATTLE", new Vector2(0, 290), new Vector2(800, 50), 34, TextAlignmentOptions.Center);
                AttachIcon(rootTr, "BossEmblem", LoadSprite("Boss_Emblem_Act1"), new Vector2(0, 110), new Vector2(200, 200));

                CreateGaugeGroup(rootTr.GetComponent<RectTransform>(), "BossHpGroup", "Icon_Attack", ColorBossHp, new Vector2(0, -60), "Boss HP: 80 / 80");
                CreateGaugeGroup(rootTr.GetComponent<RectTransform>(), "BossShieldGroup", "Icon_Shield", ColorShield, new Vector2(0, -130), "Shield: 10");

                CreateModalButton(rootTr.GetComponent<RectTransform>(), "AutoBattleNextButton", "AUTO BATTLE / NEXT", new Vector2(0, -260), new Vector2(400, 70));
            }
            go.SetActive(false);
        }

        private static void SetupMetaShopDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("MetaShopDialogPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("MetaShopDialogPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTr, false);

            MetaShopDialogView oldView = go.GetComponent<MetaShopDialogView>();
            if (oldView != null)
            {
                Object.DestroyImmediate(oldView);
            }

            ConfigureModalPanel(go.transform, new Vector2(1150, 700), ColorBgDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "ShopTitleText", "META PROGRESSION SHOP", new Vector2(0, 270), new Vector2(800, 50), 32, TextAlignmentOptions.Center);
                CreateLabel(rootTr.GetComponent<RectTransform>(), "PointsText", "POINTS: 0", new Vector2(-250, 220), new Vector2(400, 40), 24, TextAlignmentOptions.Left);
                CreateLabel(rootTr.GetComponent<RectTransform>(), "RunsText", "RUNS: 0", new Vector2(250, 220), new Vector2(400, 40), 24, TextAlignmentOptions.Right);
                CreateShopItemCard(rootTr.GetComponent<RectTransform>(), "Item1", "Initial Stamina +10\nCost: 50 Pts", new Vector2(-340, 10));
                CreateShopItemCard(rootTr.GetComponent<RectTransform>(), "Item2", "Initial Skill +5\nCost: 100 Pts", new Vector2(0, 10));
                CreateShopItemCard(rootTr.GetComponent<RectTransform>(), "Item3", "Initial Mental +15\nCost: 150 Pts", new Vector2(340, 10));
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "CloseShopButton", "CLOSE / NEXT RUN", new Vector2(0, -250), new Vector2(450, 60));
            }
            go.SetActive(false);
        }

        private static void SetupEndingPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("EndingPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(950, 650), ColorBgDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EndingTitleText", "GAME CLEAR!", new Vector2(0, 220), new Vector2(700, 60), 38, TextAlignmentOptions.Center);
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EndingDescriptionText", "You survived all 24 turns and defeated all 4 Act Bosses!\nEarned MetaPoints: +150 Pts", new Vector2(0, 60), new Vector2(700, 150), 24, TextAlignmentOptions.Center);
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "RestartButton", "RESTART / SHOP", new Vector2(0, -180), new Vector2(450, 70));
            }
            go.SetActive(false);
        }

        private static void SetupUIViews(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("UIViews");
            GameObject go = tr != null ? tr.gameObject : new GameObject("UIViews", typeof(RectTransform));
            go.transform.SetParent(canvasTr, false);
            go.SetActive(true);

            EnsureRectTransform(go);

            // EndingView, RelicDraftDialogView, BossBattleDialogView, MetaShopDialogView を常時アクティブなホストへ配置
            if (go.GetComponent<EndingView>() == null)
            {
                go.AddComponent<EndingView>();
            }
            if (go.GetComponent<RelicDraftDialogView>() == null)
            {
                go.AddComponent<RelicDraftDialogView>();
            }
            if (go.GetComponent<BossBattleDialogView>() == null)
            {
                go.AddComponent<BossBattleDialogView>();
            }
            if (go.GetComponent<MetaShopDialogView>() == null)
            {
                go.AddComponent<MetaShopDialogView>();
            }
        }

        // --- UI 生成ヘルパー群 ---

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }
            return rect;
        }

        private static void ConfigureModalPanel(Transform panelTr, Vector2 size, Color bgColor)
        {
            EnsureRectTransform(panelTr.gameObject);
            RectTransform rect = panelTr.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image overlayImg = panelTr.GetComponent<Image>() ?? panelTr.gameObject.AddComponent<Image>();
            overlayImg.color = ColorOverlay;

            Transform rootTr = panelTr.Find("PanelRoot");
            GameObject rootGo = rootTr != null ? rootTr.gameObject : new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(panelTr, false);

            RectTransform rootRect = EnsureRectTransform(rootGo);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = size;
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImg = rootGo.GetComponent<Image>() ?? rootGo.AddComponent<Image>();
            rootImg.sprite = LoadSprite("Frame_Card");
            rootImg.type = Image.Type.Sliced;
            rootImg.color = bgColor;
        }

        private static GameObject CreateGaugeGroup(RectTransform parent, string name, string iconName, Color barColor, Vector2 pos, string label)
        {
            Transform existing = parent.Find(name);
            GameObject groupGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            groupGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(groupGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(320, 60);

            // アイコン
            AttachIcon(rect, "Icon", LoadSprite(iconName), new Vector2(-130, 0), new Vector2(40, 40));

            // ゲージ背景
            Transform barBgTr = rect.Find("BarBg");
            GameObject barBgGo = barBgTr != null ? barBgTr.gameObject : new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            barBgGo.transform.SetParent(rect, false);
            RectTransform barBgRect = EnsureRectTransform(barBgGo);
            barBgRect.anchoredPosition = new Vector2(20, -5);
            barBgRect.sizeDelta = new Vector2(220, 24);
            barBgGo.GetComponent<Image>().color = ColorBarBg;

            // ゲージバー
            Transform barFillTr = barBgRect.Find("BarFill");
            GameObject barFillGo = barFillTr != null ? barFillTr.gameObject : new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            barFillGo.transform.SetParent(barBgRect, false);
            RectTransform barFillRect = EnsureRectTransform(barFillGo);
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0.7f, 1f); // 70% 仮置き
            barFillRect.sizeDelta = Vector2.zero;
            barFillGo.GetComponent<Image>().color = barColor;

            // ラベル
            CreateLabel(rect, "Label", label, new Vector2(20, 15), new Vector2(220, 24), 18, TextAlignmentOptions.Center);
            return groupGo;
        }

        private static void CreateCommandButton(RectTransform parent, string name, string iconName, string text, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(btnGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 120);

            Image img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = ColorButtonBg;

            AttachIcon(rect, "Icon", LoadSprite(iconName), new Vector2(-80, 0), new Vector2(56, 56));
            CreateLabel(rect, "Text", text, new Vector2(40, 0), new Vector2(180, 80), 22, TextAlignmentOptions.Center);
        }

        private static void CreateModalButton(RectTransform parent, string name, string text, Vector2 pos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(btnGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.45f, 0.85f, 1.0f);

            CreateLabel(rect, "Text", text, Vector2.zero, size, 22, TextAlignmentOptions.Center);
        }

        private static RelicCardView CreateRelicCard(RectTransform parent, string name, string relicName, string desc, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            GameObject cardGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RelicCardView));
            if (cardGo.GetComponent<RelicCardView>() == null)
            {
                cardGo.AddComponent<RelicCardView>();
            }
            cardGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(cardGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280, 380);

            Image img = cardGo.GetComponent<Image>() ?? cardGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = ColorButtonBg;

            AttachIcon(rect, "Icon", LoadSprite("Icon_Relic"), new Vector2(0, 80), new Vector2(80, 80));
            TextMeshProUGUI nameLbl = CreateLabel(rect, "NameText", relicName, new Vector2(0, 0), new Vector2(240, 40), 22, TextAlignmentOptions.Center).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descLbl = CreateLabel(rect, "DescText", desc, new Vector2(0, -60), new Vector2(240, 80), 18, TextAlignmentOptions.Center).GetComponent<TextMeshProUGUI>();
            CreateModalButton(rect, "SelectButton", "SELECT", new Vector2(0, -130), new Vector2(200, 45));

            Transform btnTr = rect.Find("SelectButton");
            Button btn = btnTr != null ? btnTr.GetComponent<Button>() : null;

            RelicCardView cardView = cardGo.GetComponent<RelicCardView>();
            if (cardView != null)
            {
                SerializedObject cSo = new SerializedObject(cardView);
                cSo.FindProperty("_nameText").objectReferenceValue = nameLbl;
                cSo.FindProperty("_descriptionText").objectReferenceValue = descLbl;
                cSo.FindProperty("_selectButton").objectReferenceValue = btn;
                cSo.ApplyModifiedProperties();
            }

            return cardView;
        }

        private static void CreateShopItemCard(RectTransform parent, string cardName, string text, Vector2 pos)
        {
            Transform existing = parent.Find(cardName);
            GameObject cardGo = existing != null ? existing.gameObject : new GameObject(cardName, typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);

            RelicCardView oldCardView = cardGo.GetComponent<RelicCardView>();
            if (oldCardView != null)
            {
                Object.DestroyImmediate(oldCardView);
            }

            RectTransform rect = EnsureRectTransform(cardGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280, 420);

            Image img = cardGo.GetComponent<Image>() ?? cardGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = ColorButtonBg;

            AttachIcon(rect, "Icon", LoadSprite("Icon_Relic"), new Vector2(0, 80), new Vector2(80, 80));
            CreateLabel(rect, "NameText", cardName, new Vector2(0, 0), new Vector2(240, 40), 22, TextAlignmentOptions.Center);
            CreateLabel(rect, "DescText", text, new Vector2(0, -60), new Vector2(240, 80), 18, TextAlignmentOptions.Center);
            CreateModalButton(rect, "SelectButton", "SELECT", new Vector2(0, -130), new Vector2(200, 45));
        }

        private static void AttachIcon(Transform parent, string iconName, Sprite sprite, Vector2 anchoredPos, Vector2 size)
        {
            if (parent == null) return;
            Transform existing = parent.Find(iconName);
            GameObject iconGo = existing != null ? existing.gameObject : new GameObject(iconName, typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(iconGo);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = iconGo.GetComponent<Image>() ?? iconGo.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private static GameObject CreateLabel(RectTransform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            GameObject labelGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(labelGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>() ?? labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return labelGo;
        }

        private static void RebindGameFlowControllerReferences(Transform canvasTr)
        {
            GameFlowController controller = Object.FindFirstObjectByType<GameFlowController>();
            if (controller == null) return;

            SerializedObject so = new SerializedObject(controller);
            BindAsset<GameRulesSO>(so, "_gameRules", "Assets/Data/Rules/GameRules.asset");
            BindAsset<CommandResolverSO>(so, "_commandResolver", "Assets/Data/Commands/CommandResolver.asset");
            BindAsset<GameEventCatalogSO>(so, "_eventCatalog", "Assets/Data/Events/GameEventCatalog.asset");
            BindAsset<EventResolverSO>(so, "_eventResolver", "Assets/Data/Events/EventResolver.asset");
            BindAsset<EndingRulesSO>(so, "_endingRules", "Assets/Data/Endings/EndingRules.asset");
            BindAsset<EndingResolverSO>(so, "_endingResolver", "Assets/Data/Endings/EndingResolver.asset");
            BindAsset<GameStateEventChannelSO>(so, "_gameStateChannel", "Assets/Data/Channels/GameStateChannel.asset");
            BindAsset<GameEventFiredChannelSO>(so, "_eventFiredChannel", "Assets/Data/Channels/EventFiredChannel.asset");
            BindAsset<EndingDecidedChannelSO>(so, "_endingDecidedChannel", "Assets/Data/Channels/EndingDecidedChannel.asset");
            BindAsset<RelicCatalogSO>(so, "_relicCatalog", "Assets/Features/Relic/Instances/RelicCatalog.asset");
            BindAsset<RelicResolverSO>(so, "_relicResolver", "Assets/Features/Relic/Instances/RelicResolver.asset");
            BindAsset<BossCatalogSO>(so, "_bossCatalog", "Assets/Features/Boss/Instances/BossCatalog.asset");
            BindAsset<AutoBattleResolverSO>(so, "_autoBattleResolver", "Assets/Features/Boss/Instances/AutoBattleResolver.asset");
            BindAsset<MetaPointResolverSO>(so, "_metaPointResolver", "Assets/Features/MetaProgression/Instances/MetaPointResolver.asset");
            BindAsset<MetaUnlockCatalogSO>(so, "_metaUnlockCatalog", "Assets/Features/MetaProgression/Instances/MetaUnlockCatalog.asset");

            int[] bossBattleTurns = { 6, 12, 18, 24 };
            SerializedProperty turnsProp = so.FindProperty("_bossBattleTurns");
            turnsProp.ClearArray();
            turnsProp.arraySize = bossBattleTurns.Length;
            for (int i = 0; i < bossBattleTurns.Length; i++)
            {
                turnsProp.GetArrayElementAtIndex(i).intValue = bossBattleTurns[i];
            }

            so.ApplyModifiedProperties();

            BindStatusViewSceneReferences(canvasTr, controller);
            BindRelicDraftDialogSceneReferences(canvasTr, controller);
            BindBossBattleDialogSceneReferences(canvasTr);
            BindEndingViewSceneReferences(canvasTr, controller);
            BindMetaShopDialogSceneReferences(canvasTr, controller);
        }

        private static void BindBossBattleDialogSceneReferences(Transform canvasTr)
        {
            Transform bossPanelTr = canvasTr.Find("BossBattleDialogPanel");
            if (bossPanelTr == null)
            {
                Debug.LogError("[UILayoutBuilder] BossBattleDialogPanel not found on Canvas.");
                return;
            }

            Transform viewsTr = canvasTr.Find("UIViews");
            if (viewsTr == null)
            {
                Debug.LogError("[UILayoutBuilder] UIViews not found on Canvas for BossBattleDialogView.");
                return;
            }

            BossBattleDialogView view = viewsTr.GetComponent<BossBattleDialogView>();
            if (view == null)
            {
                Debug.LogError("[UILayoutBuilder] BossBattleDialogView component not found on UIViews.");
                return;
            }

            SerializedObject so = new SerializedObject(view);

            // _panelRoot は BossBattleDialogPanel 全体（全画面モーダル背景含む）を指す
            so.FindProperty("_panelRoot").objectReferenceValue = bossPanelTr.gameObject;

            Transform rootTr = bossPanelTr.Find("PanelRoot");
            BindComponentReference<TextMeshProUGUI>(so, "_bossNameText", rootTr, "BossBattleDialogPanel", "BossTitleText", "BossNameText");
            BindComponentReference<TextMeshProUGUI>(so, "_bossHpText", rootTr, "BossBattleDialogPanel", "BossHpGroup/Label", "BossHpText");
            BindComponentReference<Slider>(so, "_bossHpSlider", rootTr, "BossBattleDialogPanel", "BossHpSlider", "BossHpGroup/BossHpSlider");
            BindComponentReference<TextMeshProUGUI>(so, "_shieldText", rootTr, "BossBattleDialogPanel", "BossShieldGroup/Label", "ShieldText");
            BindComponentReference<TextMeshProUGUI>(so, "_battleLogText", rootTr, "BossBattleDialogPanel", "BattleLogText");
            BindComponentReference<Button>(so, "_dismissButton", rootTr, "BossBattleDialogPanel", "AutoBattleNextButton", "DismissButton");
            BindComponentReference<TextMeshProUGUI>(so, "_dismissButtonText", rootTr, "BossBattleDialogPanel", "AutoBattleNextButton/Text", "DismissButton/Text", "DismissButtonText");

            so.ApplyModifiedProperties();
        }

        private static void BindComponentReference<T>(SerializedObject so, string propName, Transform rootTr, string containerName, params string[] candidatePaths) where T : Component
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogError($"[UILayoutBuilder] Serialized property not found: {propName}");
                return;
            }

            if (rootTr == null)
            {
                Debug.LogError($"[UILayoutBuilder] Root transform is null, cannot bind {propName}. Existing reference is kept.");
                return;
            }

            T comp = null;
            foreach (string path in candidatePaths)
            {
                Transform targetTr = rootTr.Find(path);
                if (targetTr != null)
                {
                    comp = targetTr.GetComponent<T>();
                    if (comp != null)
                    {
                        break;
                    }
                }
            }

            if (comp != null)
            {
                prop.objectReferenceValue = comp;
            }
            else
            {
                Debug.LogError($"[UILayoutBuilder] Element ({typeof(T).Name}) not found for '{propName}' (searched: {string.Join(", ", candidatePaths)}) under {containerName}. Existing reference is kept.");
            }
        }


        // StatusView はボス戦の発生を GameFlowController から受け取り、BossBattleDialogView へ橋渡しする。
        // どちらもシーン内オブジェクトなので、全パネル生成後にまとめて結線する。
        private static void BindStatusViewSceneReferences(Transform canvasTr, GameFlowController controller)
        {
            Transform statusTr = canvasTr.Find("StatusPanel");
            if (statusTr == null) return;

            StatusView view = statusTr.GetComponent<StatusView>();
            if (view == null) return;

            Transform viewsTr = canvasTr.Find("UIViews");
            BossBattleDialogView bossDialog = viewsTr != null ? viewsTr.GetComponent<BossBattleDialogView>() : null;

            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_gameFlowController").objectReferenceValue = controller;
            so.FindProperty("_bossBattleDialog").objectReferenceValue = bossDialog;

            Transform pointsTr = statusTr.Find("PointsText");
            if (pointsTr != null)
            {
                so.FindProperty("_metaPointsText").objectReferenceValue = pointsTr.GetComponent<TextMeshProUGUI>();
            }

            so.ApplyModifiedProperties();
        }

        private static void BindMetaShopDialogSceneReferences(Transform canvasTr, GameFlowController controller)
        {
            Transform shopPanelTr = canvasTr.Find("MetaShopDialogPanel");
            if (shopPanelTr == null)
            {
                Debug.LogError("[UILayoutBuilder] MetaShopDialogPanel not found on Canvas.");
                return;
            }

            Transform viewsTr = canvasTr.Find("UIViews");
            if (viewsTr == null)
            {
                Debug.LogError("[UILayoutBuilder] UIViews not found on Canvas for MetaShopDialogView.");
                return;
            }

            MetaShopDialogView view = viewsTr.GetComponent<MetaShopDialogView>();
            if (view == null)
            {
                Debug.LogError("[UILayoutBuilder] MetaShopDialogView component not found on UIViews.");
                return;
            }

            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_panelRoot").objectReferenceValue = shopPanelTr.gameObject;
            so.FindProperty("_gameFlowController").objectReferenceValue = controller;
            BindAsset<MetaUnlockCatalogSO>(so, "_unlockCatalog", "Assets/Features/MetaProgression/Instances/MetaUnlockCatalog.asset");

            Transform rootTr = shopPanelTr.Find("PanelRoot");
            if (rootTr != null)
            {
                BindComponentReference<TextMeshProUGUI>(so, "_availablePointsText", rootTr, "MetaShopDialogPanel", "PointsText");
                BindComponentReference<TextMeshProUGUI>(so, "_totalRunsText", rootTr, "MetaShopDialogPanel", "RunsText");
                BindComponentReference<Button>(so, "_closeButton", rootTr, "MetaShopDialogPanel", "CloseShopButton");

                SerializedProperty itemButtonsProp = so.FindProperty("_itemButtons");
                SerializedProperty itemNameTextsProp = so.FindProperty("_itemNameTexts");
                SerializedProperty itemDescTextsProp = so.FindProperty("_itemDescTexts");

                itemButtonsProp.ClearArray();
                itemButtonsProp.arraySize = 3;
                itemNameTextsProp.ClearArray();
                itemNameTextsProp.arraySize = 3;
                itemDescTextsProp.ClearArray();
                itemDescTextsProp.arraySize = 3;

                string[] itemNames = { "Item1", "Item2", "Item3" };
                for (int i = 0; i < itemNames.Length; i++)
                {
                    Transform itemTr = rootTr.Find(itemNames[i]);
                    if (itemTr != null)
                    {
                        Transform btnTr = itemTr.Find("SelectButton");
                        if (btnTr != null)
                        {
                            itemButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = btnTr.GetComponent<Button>();
                        }
                        Transform nameTr = itemTr.Find("NameText");
                        if (nameTr != null)
                        {
                            itemNameTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = nameTr.GetComponent<TextMeshProUGUI>();
                        }
                        Transform descTr = itemTr.Find("DescText");
                        if (descTr != null)
                        {
                            itemDescTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = descTr.GetComponent<TextMeshProUGUI>();
                        }
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        private static void BindRelicDraftDialogSceneReferences(Transform canvasTr, GameFlowController controller)
        {
            Transform viewsTr = canvasTr.Find("UIViews");
            if (viewsTr == null)
            {
                Debug.LogError("[UILayoutBuilder] UIViews not found on Canvas for RelicDraftDialogView.");
                return;
            }

            RelicDraftDialogView view = viewsTr.GetComponent<RelicDraftDialogView>();
            if (view == null)
            {
                Debug.LogError("[UILayoutBuilder] RelicDraftDialogView component not found on UIViews.");
                return;
            }

            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_gameFlowController").objectReferenceValue = controller;

            Transform panelTr = canvasTr.Find("RelicDraftDialogPanel");
            if (panelTr != null)
            {
                so.FindProperty("_panelRoot").objectReferenceValue = panelTr.gameObject;
            }
            else
            {
                Debug.LogError("[UILayoutBuilder] RelicDraftDialogPanel not found for RelicDraftDialogView._panelRoot.");
            }

            BindAsset<RelicAcquiredChannelSO>(so, "_relicAcquiredChannel", "Assets/Features/Relic/Instances/RelicAcquiredChannel.asset");

            Transform rootTr = panelTr != null ? panelTr.Find("PanelRoot") : null;
            if (rootTr != null)
            {
                RelicCardView[] cardViews = rootTr.GetComponentsInChildren<RelicCardView>(true);
                SerializedProperty cardsProp = so.FindProperty("_cardViews");
                if (cardsProp != null)
                {
                    cardsProp.ClearArray();
                    for (int i = 0; i < cardViews.Length; i++)
                    {
                        cardsProp.InsertArrayElementAtIndex(i);
                        cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardViews[i];
                    }
                }
            }
            else
            {
                Debug.LogError("[UILayoutBuilder] RelicDraftDialogPanel/PanelRoot not found for RelicDraftDialogView._cardViews.");
            }

            so.ApplyModifiedProperties();
        }

        private static void BindEndingViewSceneReferences(Transform canvasTr, GameFlowController controller)
        {
            Transform viewsTr = canvasTr.Find("UIViews");
            if (viewsTr == null)
            {
                Debug.LogError("[UILayoutBuilder] UIViews not found on Canvas for EndingView.");
                return;
            }

            EndingView view = viewsTr.GetComponent<EndingView>();
            if (view == null)
            {
                Debug.LogError("[UILayoutBuilder] EndingView component not found on UIViews.");
                return;
            }

            SerializedObject so = new SerializedObject(view);

            BindAsset<EndingDecidedChannelSO>(so, "_endingDecidedChannel", "Assets/Data/Channels/EndingDecidedChannel.asset");
            so.FindProperty("_gameFlowController").objectReferenceValue = controller;

            Transform panelTr = canvasTr.Find("EndingPanel");
            if (panelTr != null)
            {
                so.FindProperty("_panelRoot").objectReferenceValue = panelTr.gameObject;
            }
            else
            {
                Debug.LogError("[UILayoutBuilder] EndingPanel not found for EndingView._panelRoot.");
            }

            Transform rootTr = panelTr != null ? panelTr.Find("PanelRoot") : null;
            if (rootTr != null)
            {
                Transform titleTr = rootTr.Find("EndingTitleText");
                TextMeshProUGUI titleTmp = titleTr != null ? titleTr.GetComponent<TextMeshProUGUI>() : null;
                if (titleTmp != null)
                {
                    so.FindProperty("_resultText").objectReferenceValue = titleTmp;
                }
                else
                {
                    Debug.LogError("[UILayoutBuilder] EndingPanel/PanelRoot/EndingTitleText (TextMeshProUGUI) not found for EndingView._resultText.");
                }

                Transform restartBtnTr = rootTr.Find("RestartButton");
                Button restartBtn = restartBtnTr != null ? restartBtnTr.GetComponent<Button>() : null;
                if (restartBtn != null)
                {
                    so.FindProperty("_restartButton").objectReferenceValue = restartBtn;
                }
                else
                {
                    Debug.LogError("[UILayoutBuilder] EndingPanel/PanelRoot/RestartButton (Button) not found for EndingView._restartButton.");
                }
            }
            else
            {
                Debug.LogError("[UILayoutBuilder] EndingPanel/PanelRoot not found for EndingView.");
            }

            so.ApplyModifiedProperties();
        }

        // 参照先が見つからないときは既存の結線を残したままエラーで知らせる。
        // 黙って null を書き込むと、シーン再構築のたびに Inspector のアサインが消えて原因を追えなくなる。
        private static void BindAsset<T>(SerializedObject so, string propName, string assetPath) where T : Object
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogError($"[UILayoutBuilder] Serialized property not found: {propName}");
                return;
            }

            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[UILayoutBuilder] Asset not found: {assetPath} ({typeof(T).Name}). Existing reference on {propName} is kept.");
                return;
            }

            prop.objectReferenceValue = asset;
        }

        private static void SetViewProperty(SerializedObject so, string propName, Transform tr)
        {
            if (tr == null) return;
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                Component view = tr.GetComponent<MonoBehaviour>();
                if (view != null)
                {
                    prop.objectReferenceValue = view;
                }
            }
        }
    }
}
#endif
