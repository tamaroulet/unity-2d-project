// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using Game.Core;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.EditorScripts
{
    /// <summary>
    /// UI Toolkit パイロット（EventDialogView）の結線を Editor API で行う。
    ///
    /// .agents/rules/00_rules.md の「Unity 固有の禁止事項」に従い、シーンの変更は
    /// テキスト編集ではなく Unity のシリアライズ機構を通す。本スクリプトはその
    /// 2 つ目の経路（Editor スクリプトを Unity から走らせる）にあたる。
    ///
    /// 冪等である。既にコンポーネントが付いていれば追加せず、参照だけ張り直す。
    /// </summary>
    public static class EventDialogUIBinder
    {
        private const string ScenePath = "Assets/Scenes/MainGame.unity";
        private const string UxmlPath = "Assets/UI/UXML/EventDialog.uxml";
        private const string ChannelPath = "Assets/Data/Channels/EventFiredChannel.asset";
        private const string PanelName = "EventDialogPanel";

        [MenuItem("Tools/Bind EventDialog UI Toolkit")]
        public static void BindEventDialogUI()
        {
            // EditorSceneManager.OpenScene は Play 中に呼べない。
            // メニューから実行された場合に備えて明示的に弾く。
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[EventDialogUIBinder] Play モード中は実行できません。"
                    + "再生を停止してから Tools/Bind EventDialog UI Toolkit を実行してください。");
                return;
            }

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (uxml == null)
            {
                Debug.LogError($"[EventDialogUIBinder] UXML not found: {UxmlPath}");
                return;
            }

            GameEventFiredChannelSO channel =
                AssetDatabase.LoadAssetAtPath<GameEventFiredChannelSO>(ChannelPath);
            if (channel == null)
            {
                Debug.LogError($"[EventDialogUIBinder] Event channel not found: {ChannelPath}");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject panel = FindInScene(PanelName);
            if (panel == null)
            {
                Debug.LogError($"[EventDialogUIBinder] GameObject not found in scene: {PanelName}");
                return;
            }

            UIDocument document = panel.GetComponent<UIDocument>();
            if (document == null)
            {
                document = Undo.AddComponent<UIDocument>(panel);
            }

            // PanelSettings が未設定だと UIDocument は何も描画しない。
            // 資産の生成は SerializedObject を作る前に済ませる。生成の中で
            // AssetDatabase.Refresh() が走ると、未適用の SerializedProperty の
            // 変更が破棄されるためである（sourceAsset が null のまま保存される事故が起きた）。
            Object panelSettings = FindFirstAssetOfType("PanelSettings")
                                   ?? CreateDefaultPanelSettings();
            if (panelSettings == null)
            {
                Debug.LogWarning("[EventDialogUIBinder] PanelSettings could not be created. "
                    + "The UI will not render until one is assigned to the UIDocument.");
            }

            SerializedObject docSo = new SerializedObject(document);
            SerializedProperty sourceProp = docSo.FindProperty("sourceAsset");
            if (sourceProp == null)
            {
                Debug.LogError("[EventDialogUIBinder] UIDocument has no 'sourceAsset' property. "
                    + "The Unity version may have renamed it.");
                return;
            }
            sourceProp.objectReferenceValue = uxml;

            SerializedProperty panelSettingsProp = docSo.FindProperty("m_PanelSettings");
            if (panelSettingsProp != null && panelSettingsProp.objectReferenceValue == null
                && panelSettings != null)
            {
                panelSettingsProp.objectReferenceValue = panelSettings;
            }
            docSo.ApplyModifiedPropertiesWithoutUndo();

            // 書き込めたことを確認する。SerializedObject は黙って落ちることがある
            docSo.Update();
            if (docSo.FindProperty("sourceAsset").objectReferenceValue == null)
            {
                Debug.LogError("[EventDialogUIBinder] sourceAsset remained null after apply. "
                    + "The UXML was not bound.");
                return;
            }

            EventDialogViewUI view = panel.GetComponent<EventDialogViewUI>();
            if (view == null)
            {
                view = Undo.AddComponent<EventDialogViewUI>(panel);
            }

            SerializedObject viewSo = new SerializedObject(view);
            viewSo.FindProperty("_uiDocument").objectReferenceValue = document;
            viewSo.FindProperty("_eventFiredChannel").objectReferenceValue = channel;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // 旧 uGUI 実装は残すが、同一パネル上で二重に購読しないよう無効化する。
            EventDialogView legacy = panel.GetComponent<EventDialogView>();
            if (legacy != null && legacy.enabled)
            {
                legacy.enabled = false;
                EditorUtility.SetDirty(legacy);
                Debug.Log("[EventDialogUIBinder] Disabled legacy EventDialogView component.");
            }

            EditorUtility.SetDirty(document);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(panel.scene);
            EditorSceneManager.SaveScene(panel.scene);

            Debug.Log($"[EventDialogUIBinder] Bound {UxmlPath} to {PanelName} and saved {ScenePath}.");
        }

        /// <summary>非アクティブなオブジェクトも辿るため、ルートから名前で降りていく。</summary>
        private static GameObject FindInScene(string name)
        {
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t.gameObject;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// PanelSettings が 1 つも無ければ既定値で作る。
        /// これが無いと UIDocument は何も描画せず、「結線したのに何も出ない」という
        /// 原因の分かりにくい状態になる。UI Toolkit 導入時の典型的な落とし穴である。
        /// </summary>
        private static Object CreateDefaultPanelSettings()
        {
            const string dir = "Assets/UI/Settings";
            const string path = dir + "/DefaultPanelSettings.asset";

            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/UI", "Settings");
            }

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EventDialogUIBinder] Created {path} (none existed).");
            return AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        }

        private static Object FindFirstAssetOfType(string typeName)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}");
            if (guids.Length == 0)
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
#endif
