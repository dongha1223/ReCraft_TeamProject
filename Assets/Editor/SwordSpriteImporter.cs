using UnityEditor;
using UnityEngine;
using System.IO;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검기 스프라이트 시트 임포터 및 설정 자동화 에디터 툴
    /// Window > ReCraft > Import Sword Energy Sprites 로 실행
    /// </summary>
    public class SwordSpriteImporter : EditorWindow
    {
        private const string SOURCE_PATH  = "/mnt/user-data/uploads/PcL5kTqE.png";
        private const string TARGET_PATH  = "Assets/Sprites/Skills/SwordEnergy_Sheet.png";
        private const string ANIM_FOLDER  = "Assets/Animations/Skills";
        private const string ANIM_PATH    = "Assets/Animations/Skills/SwordEnergy.anim";

        // 스프라이트 시트 정보: 3열 × 2행 = 6프레임
        private const int COLS       = 3;
        private const int ROWS       = 2;
        private const int FRAME_W    = 683;   // 2048 / 3 ≈ 683
        private const int FRAME_H    = 410;   // 820 / 2 = 410

        [MenuItem("Window/ReCraft/Import Sword Energy Sprites")]
        public static void Run()
        {
            // ── 1. PNG 복사 ────────────────────────────────────────────
            if (!File.Exists(SOURCE_PATH))
            {
                Debug.LogError($"[SwordImporter] 파일 없음: {SOURCE_PATH}");
                return;
            }

            string destFull = Path.Combine(Application.dataPath, "../", TARGET_PATH);
            destFull = Path.GetFullPath(destFull);
            Directory.CreateDirectory(Path.GetDirectoryName(destFull));
            File.Copy(SOURCE_PATH, destFull, overwrite: true);
            Debug.Log($"[SwordImporter] 복사 완료: {destFull}");

            AssetDatabase.ImportAsset(TARGET_PATH, ImportAssetOptions.ForceUpdate);

            // ── 2. 텍스처 임포트 설정 ─────────────────────────────────
            var ti = (TextureImporter)AssetImporter.GetAtPath(TARGET_PATH);
            if (ti == null) { Debug.LogError("[SwordImporter] TextureImporter null"); return; }

            ti.textureType        = TextureImporterType.Sprite;
            ti.spriteImportMode   = SpriteImportMode.Multiple;
            ti.filterMode         = FilterMode.Bilinear;
            ti.mipmapEnabled      = false;
            ti.alphaIsTransparency = true;
            ti.sRGBTexture        = true;

            // 실제 픽셀 크기로 재계산
            Texture2D tmpTex = new Texture2D(2, 2);
            tmpTex.LoadImage(File.ReadAllBytes(destFull));
            int texW = tmpTex.width, texH = tmpTex.height;
            DestroyImmediate(tmpTex);
            Debug.Log($"[SwordImporter] 텍스처 크기: {texW} x {texH}");

            int fw = texW / COLS;
            int fh = texH / ROWS;

            // ── 3. 스프라이트 메타데이터 슬라이스 ─────────────────────
            var metas = new SpriteMetaData[COLS * ROWS];
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    int idx = r * COLS + c;
                    // Unity sprite rect: Y는 아래부터 시작
                    int x = c * fw;
                    int y = texH - (r + 1) * fh;
                    metas[idx] = new SpriteMetaData
                    {
                        name   = $"SwordEnergy_{idx + 1:00}",
                        rect   = new Rect(x, y, fw, fh),
                        pivot  = new Vector2(0.5f, 0.5f),
                        alignment = 9   // custom pivot
                    };
                }
            }
            ti.spritesheet = metas;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();

            // ── 4. Additive 머티리얼 생성 (검/흰 배경 → 투명) ─────────
            CreateAdditiveMaterial();

            // ── 5. AnimationClip 생성 (6프레임 재생) ─────────────────
            CreateAnimationClip();

            Debug.Log("[SwordImporter] ✅ 완료! SwordEnergy_Sheet 임포트 성공.");
            EditorUtility.DisplayDialog("SwordImporter", "검기 스프라이트 임포트 완료!", "OK");
        }

        // ── Additive 머티리얼 ─────────────────────────────────────────
        private static void CreateAdditiveMaterial()
        {
            const string matPath = "Assets/Sprites/Skills/SwordEnergy_Additive.mat";

            var mat = new Material(Shader.Find("Sprites/Default"));
            // Additive 블렌딩: 검은색 = 완전 투명, 밝은 색 = 발광
            mat.SetFloat("_Mode", 3);
            mat.EnableKeyword("_ALPHABLEND_ON");

            // Blend SrcAlpha One : Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;

            Directory.CreateDirectory(Path.GetDirectoryName(
                Path.GetFullPath(Path.Combine(Application.dataPath, "../", matPath))));
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SwordImporter] 머티리얼 생성: {matPath}");
        }

        // ── AnimationClip 생성 ────────────────────────────────────────
        private static void CreateAnimationClip()
        {
            string animFolderFull = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../", ANIM_FOLDER));
            Directory.CreateDirectory(animFolderFull);

            // 스프라이트 로드
            var sprites = AssetDatabase.LoadAllAssetsAtPath(TARGET_PATH);
            var spriteList = new System.Collections.Generic.List<Sprite>();
            for (int i = 1; i <= 6; i++)
            {
                foreach (var obj in sprites)
                    if (obj is Sprite s && s.name == $"SwordEnergy_{i:00}")
                        spriteList.Add(s);
            }

            if (spriteList.Count == 0)
            {
                Debug.LogWarning("[SwordImporter] 스프라이트를 찾지 못했습니다. 나중에 애니메이션 재생성 필요.");
                return;
            }

            var clip = new AnimationClip();
            clip.frameRate = 12;
            AnimationUtility.SetAnimationClipSettings(clip,
                new AnimationClipSettings { loopTime = true });

            var binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = "",
                propertyName = "m_Sprite"
            };

            float fps     = clip.frameRate;
            var   frames  = new ObjectReferenceKeyframe[spriteList.Count];
            for (int i = 0; i < spriteList.Count; i++)
                frames[i] = new ObjectReferenceKeyframe
                {
                    time  = i / fps,
                    value = spriteList[i]
                };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            AssetDatabase.CreateAsset(clip, ANIM_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SwordImporter] 애니메이션 생성: {ANIM_PATH}  ({spriteList.Count}프레임)");
        }
    }
}
