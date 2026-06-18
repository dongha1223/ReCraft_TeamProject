using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _2D_Roguelike
{
    public static class TileSheetSetup
    {
        private const string TexturePath   = "Assets/Sprites/Tiles/tiles_sheet.png";
        private const string TileAssetDir  = "Assets/Sprites/Tiles/TileAssets";
        private const string PaletteDir    = "Assets/Sprites/Tiles/TilePalette";
        private const int    TileSize      = 256;
        private const int    Cols          = 5;
        private const int    Rows          = 4;
        private const int    PixelsPerUnit = 256;

        private static readonly string[] RowNames = { "lava", "grass", "stone", "grave" };

        [MenuItem("Tools/Setup Tile Sheet")]
        public static void Run()
        {
            SetImportSettings();
            var tiles = CreateTileAssets();
            CreateTilePalette(tiles);
            Debug.Log($"[TileSheetSetup] Done — {tiles.Count} tiles, palette at {PaletteDir}");
        }

        // ── 1. 임포트 설정 + 슬라이싱 ────────────────────────────────────────
static void SetImportSettings()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (importer == null) { Debug.LogError($"Texture not found: {TexturePath}"); return; }

            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode          = FilterMode.Bilinear;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;

            // Unity 텍스처 Y좌표는 하단(Y=0) 기준
#pragma warning disable CS0618
            var sheet = new List<SpriteMetaData>();
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    sheet.Add(new SpriteMetaData
                    {
                        name      = $"tile_{RowNames[row]}_{col}",
                        rect      = new Rect(col * TileSize, (Rows - 1 - row) * TileSize, TileSize, TileSize),
                        pivot     = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center
                    });
                }
            }
            importer.spritesheet = sheet.ToArray();
#pragma warning restore CS0618

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        // ── 2. Tile 에셋 생성 ────────────────────────────────────────────────
        static List<Tile> CreateTileAssets()
        {
            if (!AssetDatabase.IsValidFolder(TileAssetDir))
                AssetDatabase.CreateFolder("Assets/Sprites/Tiles", "TileAssets");

            var sprites = new List<Sprite>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(TexturePath))
                if (obj is Sprite s) sprites.Add(s);

            // row, col 순서로 정렬
            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            var tiles = new List<Tile>();
            foreach (var sprite in sprites)
            {
                string path = $"{TileAssetDir}/{sprite.name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path)
                           ?? ScriptableObject.CreateInstance<Tile>();

                tile.sprite = sprite;

                if (!AssetDatabase.Contains(tile))
                    AssetDatabase.CreateAsset(tile, path);
                else
                    EditorUtility.SetDirty(tile);

                tiles.Add(tile);
            }
            AssetDatabase.SaveAssets();
            return tiles;
        }

        // ── 3. Tile Palette 생성 ─────────────────────────────────────────────
        static void CreateTilePalette(List<Tile> tiles)
        {
            if (!AssetDatabase.IsValidFolder(PaletteDir))
                AssetDatabase.CreateFolder("Assets/Sprites/Tiles", "TilePalette");

            var paletteGO = GridPaletteUtility.CreateNewPalette(
                PaletteDir, "TilePalette",
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic,
                Vector3.one,
                GridLayout.CellSwizzle.XYZ);

            if (paletteGO == null) { Debug.LogError("Failed to create Tile Palette."); return; }

            var tilemap = paletteGO.GetComponentInChildren<Tilemap>();
            if (tilemap == null) { Debug.LogError("Palette has no Tilemap child."); return; }

            // 팔레트에 타일 배치 (col 순, row 순)
            for (int i = 0; i < tiles.Count; i++)
            {
                int col = i % Cols;
                int row = i / Cols;
                tilemap.SetTile(new Vector3Int(col, -row, 0), tiles[i]);
            }

            PrefabUtility.SavePrefabAsset(paletteGO);
            AssetDatabase.SaveAssets();
        }
    }
}
