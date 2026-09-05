// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.IO;
using Game.Features.GameFlow;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorScripts
{
    public static class UiBootstrapEditorTool
    {
        private const string ScenePath = "Assets/Scenes/MainGame.unity";

        [MenuItem("Tools/Migrate EndingPanel To Runtime")]
        public static void MigrateEndingPanel()
        {
            // 1. Assets/Resources フォルダの準備とアセット移動
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string oldChannelPath = "Assets/Data/Channels/EndingDecidedChannel.asset";
            string newChannelPath = "Assets/Resources/EndingDecidedChannel.asset";

            if (File.Exists(Path.Combine(Application.dataPath, "..", oldChannelPath)))
            {
                string moveResult = AssetDatabase.MoveAsset(oldChannelPath, newChannelPath);
                if (!string.IsNullOrEmpty(moveResult))
                {
                    Debug.LogError($"[UiBootstrapEditorTool] Failed to move {oldChannelPath}: {moveResult}");
                }
                else
                {
                    Debug.Log($"[UiBootstrapEditorTool] Successfully moved {oldChannelPath} -> {newChannelPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. シーンの読み込み
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 3. Canvas と UIViews の探索
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UiBootstrapEditorTool] Canvas not found in scene.");
                return;
            }

            Transform endingPanelTr = canvas.transform.Find("EndingPanel");
            if (endingPanelTr != null)
            {
                Object.DestroyImmediate(endingPanelTr.gameObject);
                Debug.Log("[UiBootstrapEditorTool] Removed EndingPanel from Canvas.");
            }

            // 4. UiBootstrapper を Canvas に配置
            UiBootstrapper bootstrapper = canvas.GetComponent<UiBootstrapper>();
            if (bootstrapper == null)
            {
                bootstrapper = canvas.gameObject.AddComponent<UiBootstrapper>();
            }

            EndingView endingView = canvas.GetComponentInChildren<EndingView>(true);
            GameFlowController flowController = Object.FindFirstObjectByType<GameFlowController>();

            SerializedObject so = new SerializedObject(bootstrapper);
            so.FindProperty("_canvas").objectReferenceValue = canvas;
            so.FindProperty("_endingView").objectReferenceValue = endingView;
            so.FindProperty("_gameFlowController").objectReferenceValue = flowController;
            so.ApplyModifiedProperties();

            // 5. シーン保存とスナップショット更新
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UiBootstrapEditorTool] MainGame scene updated and saved.");

            SceneBindingReport.GenerateSceneSnapshot();
        }
    }
}
#endif
