// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
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

            // EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
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

            // 6. GameFlowController へのバインド
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
            CreateLabel(rect, "PointsText", "POINTS: 0", new Vector2(750, 0), new Vector2(200, 50), 28, TextAlignmentOptions.Right);

            // StatusView の SerializedObject バインド
            GameStateEventChannelSO channel = AssetDatabase.LoadAssetAtPath<GameStateEventChannelSO>("Assets/Data/Channels/GameStateEventChannel.asset");
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_gameStateChannel").objectReferenceValue = channel;
            so.FindProperty("_turnText").objectReferenceValue = turnGo.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_staminaText").objectReferenceValue = staminaGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_skillText").objectReferenceValue = skillGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            so.FindProperty("_mentalText").objectReferenceValue = mentalGo.transform.Find("Label").GetComponent<TextMeshProUGUI>();
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

            // 3つのコマンドボタン（勉強、特訓、休息）を完全配線
            SetupCommandButton(rect, "StudyButton", "Icon_Study", "勉強 (Study)\nSkill+5", new Vector2(-400, 0), "Assets/Data/Commands/Study.asset", flowController);
            SetupCommandButton(rect, "TrainButton", "Icon_Train", "特訓 (Train)\nSkill+10", new Vector2(0, 0), "Assets/Data/Commands/Train.asset", flowController);
            SetupCommandButton(rect, "RestButton", "Icon_Rest", "休息 (Rest)\nStamina+30", new Vector2(400, 0), "Assets/Data/Commands/Rest.asset", flowController);
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
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EventTitleText", "イベント発生", new Vector2(0, 180), new Vector2(700, 50), 32, TextAlignmentOptions.Center);
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EventDescriptionText", "ランダムな育成イベントが発生しました。\n選択肢を選んで能力を伸ばしましょう。", new Vector2(0, 50), new Vector2(700, 120), 22, TextAlignmentOptions.Center);
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "OptionAButton", "選択肢 A (Stamina消費 / Skill上昇)", new Vector2(0, -90), new Vector2(600, 60));
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "OptionBButton", "選択肢 B (安全策 / Mental保護)", new Vector2(0, -170), new Vector2(600, 60));
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
                CreateLabel(rootTr.GetComponent<RectTransform>(), "DraftTitleText", "レリックドラフト（パッシブ選択）", new Vector2(0, 240), new Vector2(800, 50), 32, TextAlignmentOptions.Center);
                CreateRelicCard(rootTr.GetComponent<RectTransform>(), "Card1", "鉄のダンベル\n毎ターンStamina+5", new Vector2(-340, -20));
                CreateRelicCard(rootTr.GetComponent<RectTransform>(), "Card2", "知恵の書\nSkill獲得量+20%", new Vector2(0, -20));
                CreateRelicCard(rootTr.GetComponent<RectTransform>(), "Card3", "癒やしの護符\nMental保護+30%", new Vector2(340, -20));
            }
            go.SetActive(false);
        }

        private static void SetupBossBattleDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("BossBattleDialogPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("BossBattleDialogPanel", typeof(RectTransform), typeof(Image), typeof(BossBattleDialogView));
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(1250, 750), ColorBgBossDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "BossTitleText", "⚠️ ボスバトル発生 ⚠️", new Vector2(0, 290), new Vector2(800, 50), 34, TextAlignmentOptions.Center);
                AttachIcon(rootTr, "BossEmblem", LoadSprite("Boss_Emblem_Act1"), new Vector2(0, 110), new Vector2(200, 200));

                CreateGaugeGroup(rootTr.GetComponent<RectTransform>(), "BossHpGroup", "Icon_Attack", ColorBossHp, new Vector2(0, -60), "Boss HP: 80 / 80");
                CreateGaugeGroup(rootTr.GetComponent<RectTransform>(), "BossShieldGroup", "Icon_Shield", ColorShield, new Vector2(0, -130), "Shield: 10");

                CreateModalButton(rootTr.GetComponent<RectTransform>(), "AutoBattleNextButton", "オート戦闘 進行", new Vector2(0, -260), new Vector2(400, 70));
            }
            go.SetActive(false);
        }

        private static void SetupMetaShopDialogPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("MetaShopDialogPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("MetaShopDialogPanel", typeof(RectTransform), typeof(Image), typeof(MetaShopDialogView));
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(1150, 700), ColorBgDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "ShopTitleText", "周回メタアンロックショップ", new Vector2(0, 270), new Vector2(800, 50), 32, TextAlignmentOptions.Center);
                CreateShopItemCard(rootTr.GetComponent<RectTransform>(), "Item1", "初期Stamina +10\nコスト: 50 Pts", new Vector2(-340, 30));
                CreateShopItemCard(rootTr.GetComponent<RectTransform>(), "Item2", "初期Skill +5\nコスト: 100 Pts", new Vector2(0, 30));
                CreateShopItemCard(rootTr.GetComponent<RectTransform>(), "Item3", "初期Mental +15\nコスト: 150 Pts", new Vector2(340, 30));
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "CloseShopButton", "ショップを閉じる / 次のランへ", new Vector2(0, -250), new Vector2(450, 60));
            }
            go.SetActive(false);
        }

        private static void SetupEndingPanel(Transform canvasTr)
        {
            Transform tr = canvasTr.Find("EndingPanel");
            GameObject go = tr != null ? tr.gameObject : new GameObject("EndingPanel", typeof(RectTransform), typeof(Image), typeof(EndingView));
            go.transform.SetParent(canvasTr, false);

            ConfigureModalPanel(go.transform, new Vector2(950, 650), ColorBgDialog);
            Transform rootTr = go.transform.Find("PanelRoot");
            if (rootTr != null)
            {
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EndingTitleText", "🏆 ゲームクリア！", new Vector2(0, 220), new Vector2(700, 60), 38, TextAlignmentOptions.Center);
                CreateLabel(rootTr.GetComponent<RectTransform>(), "EndingDescriptionText", "24ターンを生き抜き、全4幕のボスを撃破しました！\n獲得 MetaPoints: +150 Pts", new Vector2(0, 60), new Vector2(700, 150), 24, TextAlignmentOptions.Center);
                CreateModalButton(rootTr.GetComponent<RectTransform>(), "RestartButton", "再挑戦 / メタショップへ", new Vector2(0, -180), new Vector2(450, 70));
            }
            go.SetActive(false);
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

        private static void CreateRelicCard(RectTransform parent, string name, string text, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            GameObject cardGo = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(parent, false);

            RectTransform rect = EnsureRectTransform(cardGo);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280, 380);

            Image img = cardGo.GetComponent<Image>() ?? cardGo.AddComponent<Image>();
            img.sprite = LoadSprite("Frame_Card");
            img.type = Image.Type.Sliced;
            img.color = ColorButtonBg;

            AttachIcon(rect, "Icon", LoadSprite("Icon_Relic"), new Vector2(0, 80), new Vector2(80, 80));
            CreateLabel(rect, "Text", text, new Vector2(0, -60), new Vector2(240, 120), 20, TextAlignmentOptions.Center);
            CreateModalButton(rect, "SelectButton", "選択する", new Vector2(0, -130), new Vector2(200, 45));
        }

        private static void CreateShopItemCard(RectTransform parent, string name, string text, Vector2 pos)
        {
            CreateRelicCard(parent, name, text, pos);
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
            so.FindProperty("_gameRules").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameRulesSO>("Assets/Data/Rules/GameRules.asset");
            so.FindProperty("_commandResolver").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CommandResolverSO>("Assets/Data/Commands/CommandResolver.asset");
            so.FindProperty("_eventCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameEventCatalogSO>("Assets/Data/Events/GameEventCatalog.asset");
            so.FindProperty("_eventResolver").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EventResolverSO>("Assets/Data/Events/EventResolver.asset");
            so.FindProperty("_endingRules").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EndingRulesSO>("Assets/Data/Endings/EndingRules.asset");
            so.FindProperty("_endingResolver").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EndingResolverSO>("Assets/Data/Endings/EndingResolver.asset");
            so.FindProperty("_gameStateChannel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameStateEventChannelSO>("Assets/Data/Channels/GameStateEventChannel.asset");
            so.FindProperty("_eventFiredChannel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameEventFiredChannelSO>("Assets/Data/Channels/GameEventFiredChannel.asset");
            so.FindProperty("_endingDecidedChannel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EndingDecidedChannelSO>("Assets/Data/Channels/EndingDecidedChannel.asset");
            so.FindProperty("_relicCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RelicCatalogSO>("Assets/Features/Relic/Instances/RelicCatalog.asset");
            so.FindProperty("_relicResolver").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RelicResolverSO>("Assets/Features/Relic/Instances/RelicResolver.asset");
            so.FindProperty("_bossCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossCatalogSO>("Assets/Features/Boss/Instances/BossCatalog.asset");
            so.FindProperty("_autoBattleResolver").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AutoBattleResolverSO>("Assets/Features/Boss/Instances/AutoBattleResolver.asset");
            so.FindProperty("_metaPointResolver").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MetaPointResolverSO>("Assets/Features/MetaProgression/Instances/MetaPointResolver.asset");
            so.FindProperty("_metaUnlockCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MetaUnlockCatalogSO>("Assets/Features/MetaProgression/Instances/MetaUnlockCatalog.asset");

            int[] bossBattleTurns = { 6, 12, 18, 24 };
            SerializedProperty turnsProp = so.FindProperty("_bossBattleTurns");
            turnsProp.ClearArray();
            turnsProp.arraySize = bossBattleTurns.Length;
            for (int i = 0; i < bossBattleTurns.Length; i++)
            {
                turnsProp.GetArrayElementAtIndex(i).intValue = bossBattleTurns[i];
            }

            so.ApplyModifiedProperties();
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
