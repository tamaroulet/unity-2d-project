// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class ProceduralSpriteGenerator
    {
        private const string SpriteDir = "Assets/UI/Sprites";

        [MenuItem("Tools/Generate Procedural UI Sprites")]
        public static void GenerateAllSprites()
        {
            string fullPath = Path.Combine(Application.dataPath, "UI/Sprites");
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // 1. 基本アイコン (128x128)
            SaveTexture("Icon_Stamina", CreateStaminaIcon(128, 128));
            SaveTexture("Icon_Skill", CreateSkillIcon(128, 128));
            SaveTexture("Icon_Mental", CreateMentalIcon(128, 128));
            SaveTexture("Icon_Attack", CreateAttackIcon(128, 128));
            SaveTexture("Icon_Shield", CreateShieldIcon(128, 128));
            SaveTexture("Icon_Study", CreateStudyIcon(128, 128));
            SaveTexture("Icon_Train", CreateTrainIcon(128, 128));
            SaveTexture("Icon_Rest", CreateRestIcon(128, 128));
            SaveTexture("Icon_Relic", CreateRelicIcon(128, 128));

            // 2. ボス幾何学エンブレム (256x256)
            SaveTexture("Boss_Emblem_Act1", CreateBossEmblem(256, new Color(0.9f, 0.4f, 0.2f), 3)); // 三角形
            SaveTexture("Boss_Emblem_Act2", CreateBossEmblem(256, new Color(0.8f, 0.2f, 0.5f), 4)); // 四角形
            SaveTexture("Boss_Emblem_Act3", CreateBossEmblem(256, new Color(0.3f, 0.6f, 0.9f), 5)); // 五角形
            SaveTexture("Boss_Emblem_Act4", CreateBossEmblem(256, new Color(0.9f, 0.2f, 0.2f), 6)); // 六角形

            // 3. UI 枠・ゲージ (128x128 / 64x64)
            SaveTexture("Frame_Card", CreateCardFrame(256, 320, 16));
            SaveTexture("Bar_Fill", CreateBarFill(64, 64));

            AssetDatabase.Refresh();
            ConfigureImporterSettings();
            Debug.Log("[ProceduralSpriteGenerator] All procedural UI sprites generated and configured successfully.");
        }

        private static void SaveTexture(string name, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            string path = $"{SpriteDir}/{name}.png";
            string fullPath = Path.Combine(Application.dataPath, $"UI/Sprites/{name}.png");
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureImporterSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }
        }

        // --- 幾何学テクスチャ生成ヘルパー ---

        private static Texture2D CreateClearTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static Texture2D CreateStaminaIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.22f, 0.85f, 0.45f, 1.0f);
            // 雷（ライトニング）ボルト形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float ny = (float)y / h;
                    if ((ny > 0.45f && nx > 0.35f + (ny - 0.45f) * 0.4f && nx < 0.65f + (ny - 0.45f) * 0.4f) ||
                        (ny <= 0.55f && nx > 0.25f + ny * 0.4f && nx < 0.55f + ny * 0.4f))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSkillIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.25f, 0.65f, 0.98f, 1.0f);
            // ダイヤモンド・本形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x - w * 0.5f) / (w * 0.4f));
                    float dy = Mathf.Abs((y - h * 0.5f) / (h * 0.4f));
                    if (dx + dy <= 1.0f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMentalIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.92f, 0.35f, 0.65f, 1.0f);
            // ハート形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (x - w * 0.5f) / (w * 0.4f);
                    float py = (y - h * 0.4f) / (h * 0.4f);
                    float a = px * px + py * py - 1.0f;
                    if (a * a * a - px * px * py * py * py <= 0.0f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateAttackIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.95f, 0.30f, 0.30f, 1.0f);
            // 剣（クロスブレード）形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d1 = Mathf.Abs(x - y);
                    float d2 = Mathf.Abs(x - (h - y));
                    if ((d1 < w * 0.08f && x > w * 0.15f && x < w * 0.85f) ||
                        (d2 < w * 0.08f && x > w * 0.15f && x < w * 0.85f))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateShieldIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.30f, 0.75f, 0.95f, 1.0f);
            // 盾形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.5f) / (w * 0.4f));
                    float ny = (float)y / h;
                    if (ny > 0.4f && nx <= 0.85f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else if (ny <= 0.4f && nx <= 0.85f * (ny / 0.4f))
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStudyIcon(int w, int h)
        {
            return CreateSkillIcon(w, h);
        }

        private static Texture2D CreateTrainIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.95f, 0.65f, 0.20f, 1.0f);
            // ダンベル形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float ny = Mathf.Abs((y - h * 0.5f) / (h * 0.4f));
                    if ((nx < 0.25f || nx > 0.75f) && ny < 0.8f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                    else if (nx >= 0.25f && nx <= 0.75f && ny < 0.25f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRestIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.40f, 0.85f, 0.75f, 1.0f);
            // カップ形状
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((x - w * 0.45f) / (w * 0.35f));
                    float ny = (float)y / h;
                    if (ny > 0.25f && ny < 0.75f && nx < 0.8f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRelicIcon(int w, int h)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color col = new Color(0.95f, 0.80f, 0.25f, 1.0f);
            // 八角形クリスタル
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w * 0.5f);
                    float dy = Mathf.Abs(y - h * 0.5f);
                    if (dx < w * 0.38f && dy < h * 0.38f && (dx + dy) < w * 0.55f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBossEmblem(int size, Color mainColor, int sides)
        {
            Texture2D tex = CreateClearTexture(size, size);
            float radius = size * 0.40f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float dist = p.magnitude;
                    if (dist < radius && dist > radius * 0.75f)
                    {
                        tex.SetPixel(x, y, mainColor);
                    }
                    else if (dist < radius * 0.45f)
                    {
                        tex.SetPixel(x, y, mainColor * 0.8f);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCardFrame(int w, int h, int radius)
        {
            Texture2D tex = CreateClearTexture(w, h);
            Color bg = new Color(0.16f, 0.19f, 0.26f, 0.95f);
            Color border = new Color(0.35f, 0.42f, 0.55f, 1.0f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < 3 || x >= w - 3 || y < 3 || y >= h - 3);
                    tex.SetPixel(x, y, isBorder ? border : bg);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBarFill(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] white = new Color[w * h];
            for (int i = 0; i < white.Length; i++) white[i] = Color.white;
            tex.SetPixels(white);
            tex.Apply();
            return tex;
        }
    }
}
#endif
