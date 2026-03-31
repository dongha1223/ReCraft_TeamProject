using UnityEngine;
using UnityEditor;

namespace _2D_Roguelike
{
    public static class SpriteChecker
    {
        [MenuItem("Tools/[96px] Check Sprite Settings")]
        public static void Check()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/mage_sprites_96/frames" });
            int ok = 0, bad = 0;
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                bool good = (imp.textureType == TextureImporterType.Sprite &&
                             Mathf.Approximately(imp.spritePixelsPerUnit, 96f) &&
                             imp.filterMode == FilterMode.Point);
                if (good) ok++;
                else
                {
                    bad++;
                    Debug.LogWarning($"[SpriteChecker] ❌ {path} | Type={imp.textureType} PPU={imp.spritePixelsPerUnit} Filter={imp.filterMode}");
                    // 자동 수정
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = 96f;
                    imp.filterMode = FilterMode.Point;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                    Debug.Log($"[SpriteChecker] ✅ 자동 수정: {System.IO.Path.GetFileName(path)}");
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[SpriteChecker] 결과 — 정상: {ok}개 / 수정됨: {bad}개 / 전체: {guids.Length}개");
        }
    }
}
