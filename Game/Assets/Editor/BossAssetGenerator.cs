// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Features.Boss;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class BossAssetGenerator
    {
        [MenuItem("Tools/Generate Boss Assets")]
        public static void GenerateAssets()
        {
            const string dir = "Assets/Features/Boss/Instances";
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Features/Boss/Instances")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Features/Boss/Instances"));
            }

            // 1. Boss_Act1_01
            string bossPath = $"{dir}/Boss_Act1_01.asset";
            BossSO boss = AssetDatabase.LoadAssetAtPath<BossSO>(bossPath);
            if (boss == null)
            {
                boss = ScriptableObject.CreateInstance<BossSO>();
                AssetDatabase.CreateAsset(boss, bossPath);
            }

            SerializedObject serializedBoss = new SerializedObject(boss);
            serializedBoss.FindProperty("_bossId").intValue = 1;
            serializedBoss.FindProperty("_bossName").stringValue = "Boss_Act1_01";
            serializedBoss.FindProperty("_maxHp").intValue = 100;
            serializedBoss.FindProperty("_attackPower").intValue = 20;
            serializedBoss.FindProperty("_mentalPressurePower").intValue = 15;
            serializedBoss.FindProperty("_guardShieldAmount").intValue = 10;
            serializedBoss.FindProperty("_specialAttackMultiplier").floatValue = 2.0f;

            SerializedProperty actionPatternProp = serializedBoss.FindProperty("_actionPattern");
            actionPatternProp.ClearArray();
            actionPatternProp.arraySize = 4;
            actionPatternProp.GetArrayElementAtIndex(0).enumValueIndex = (int)BossActionKind.Attack;
            actionPatternProp.GetArrayElementAtIndex(1).enumValueIndex = (int)BossActionKind.MentalPressure;
            actionPatternProp.GetArrayElementAtIndex(2).enumValueIndex = (int)BossActionKind.Guard;
            actionPatternProp.GetArrayElementAtIndex(3).enumValueIndex = (int)BossActionKind.SpecialAttack;
            serializedBoss.ApplyModifiedProperties();

            // 2. BossCatalog
            string catalogPath = $"{dir}/BossCatalog.asset";
            BossCatalogSO catalog = AssetDatabase.LoadAssetAtPath<BossCatalogSO>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BossCatalogSO>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty bossesProp = serializedCatalog.FindProperty("_bosses");
            bossesProp.ClearArray();
            bossesProp.arraySize = 1;
            bossesProp.GetArrayElementAtIndex(0).objectReferenceValue = boss;
            serializedCatalog.ApplyModifiedProperties();

            // 3. AutoBattleResolver
            string resolverPath = $"{dir}/AutoBattleResolver.asset";
            AutoBattleResolverSO resolver = AssetDatabase.LoadAssetAtPath<AutoBattleResolverSO>(resolverPath);
            if (resolver == null)
            {
                resolver = ScriptableObject.CreateInstance<AutoBattleResolverSO>();
                AssetDatabase.CreateAsset(resolver, resolverPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BossAssetGenerator] Boss assets generated successfully.");
        }
    }
}
#endif
