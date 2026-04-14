
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class DungeonTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Textures/Dungeons/")) return;
        
        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.spritePixelsPerUnit = 16;
        ti.maxTextureSize = 64;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
#endif
