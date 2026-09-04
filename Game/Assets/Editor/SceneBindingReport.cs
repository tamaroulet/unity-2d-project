// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorScripts
{
    public static class SceneBindingReport
    {
        [MenuItem("Tools/Report Unbound Serialized Fields")]
        public static void ReportUnboundSerializedFields()
        {
            string scenePath = "Assets/Scenes/MainGame.unity";
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            List<MonoBehaviour> uiComponents = new List<MonoBehaviour>();

            foreach (GameObject root in rootObjects)
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour mb in behaviours)
                {
                    if (mb != null && mb.GetType().Namespace == "Game.UI")
                    {
                        uiComponents.Add(mb);
                    }
                }
            }

            int unboundCount = 0;
            List<string> reportLines = new List<string>();

            foreach (MonoBehaviour mb in uiComponents)
            {
                Type type = mb.GetType();
                while (type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour) && type != typeof(Component) && type != typeof(UnityEngine.Object))
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    foreach (FieldInfo field in fields)
                    {
                        bool isSerialized = field.IsPublic || field.IsDefined(typeof(SerializeField), true);
                        if (!isSerialized) continue;

                        if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                        {
                            UnityEngine.Object val = field.GetValue(mb) as UnityEngine.Object;
                            if (val == null)
                            {
                                unboundCount++;
                                reportLines.Add($"{mb.GetType().Name} / {field.Name}");
                            }
                        }
                    }
                    type = type.BaseType;
                }
            }

            Debug.Log($"[SceneBindingReport] === Total Unbound Fields: {unboundCount} ===\n" +
                      string.Join("\n", reportLines));
        }
    }
}
#endif
