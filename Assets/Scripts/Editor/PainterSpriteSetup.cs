using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace _2D_Roguelike
{
    public static class PainterSpriteSetup
    {
        [MenuItem("Tools/2D Roguelike/Setup Painter Sprite & Anim")]
        public static void Setup()
        {
            SliceSprite();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateAnim();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PainterSpriteSetup] 완료!");
            EditorUtility.DisplayDialog("완료", "화가 스프라이트 슬라이싱 + 애니메이션 설정 완료!", "확인");
        }

        private static void SliceSprite()
        {
            string path = "Assets/Sprites/Painter/신비화가.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { Debug.LogError("TextureImporter not found: " + path); return; }

            importer.textureType        = TextureImporterType.Sprite;
            importer.spriteImportMode   = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.filterMode         = FilterMode.Point;
            importer.mipmapEnabled      = false;

            // 4열 × 3행 = 12프레임, 각 128×128px
            // Unity rect: bottom-left origin
            int cols = 4, rows = 3, fw = 128, fh = 128, imgH = 384;
            var sprites = new List<SpriteMetaData>();
            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    sprites.Add(new SpriteMetaData
                    {
                        name      = $"신비화가_{idx++}",
                        rect      = new Rect(c * fw, imgH - (r + 1) * fh, fw, fh),
                        pivot     = new Vector2(0.5f, 0.5f),
                        alignment = 0
                    });
                }
            }

            importer.spritesheet = sprites.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[PainterSpriteSetup] 슬라이싱 완료: {sprites.Count}프레임");
        }

        private static void UpdateAnim()
        {
            // 슬라이싱 후 실제 sprite fileID를 anim에 반영
            string texPath  = "Assets/Sprites/Painter/신비화가.png";
            string animPath = "Assets/Sprites/Painter/Painter_Idle.anim";

            var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath);
            var spriteList = new List<Sprite>();
            foreach (var s in sprites)
                if (s is Sprite sp) spriteList.Add(sp);

            // 이름순 정렬 (신비화가_0 ~ 신비화가_11)
            spriteList.Sort((a, b) =>
            {
                int ai = int.Parse(a.name.Replace("신비화가_", ""));
                int bi = int.Parse(b.name.Replace("신비화가_", ""));
                return ai.CompareTo(bi);
            });

            if (spriteList.Count < 12)
            {
                Debug.LogError($"스프라이트 수 부족: {spriteList.Count}");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            if (clip == null) { Debug.LogError("AnimationClip not found: " + animPath); return; }

            // 기존 바인딩 제거 후 새로 설정
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, b, null);
            foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, b, null);

            var binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = "",
                propertyName = "m_Sprite"
            };

            float fps      = 8f;
            float interval = 1f / fps;
            int   count    = 12;

            var keyframes = new ObjectReferenceKeyframe[count];
            for (int i = 0; i < count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time  = i * interval,
                    value = spriteList[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            Debug.Log("[PainterSpriteSetup] 애니메이션 키프레임 설정 완료");
        }
    }
}
