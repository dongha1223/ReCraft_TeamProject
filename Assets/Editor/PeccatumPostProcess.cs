
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

public class PeccatumPostProcess
{
    static readonly (int buildIndex, string sceneName, float bloomInt, float vigR, float vigG, float vigB, float vigInt, float contrastVal, float satVal, float filterR, float filterG, float filterB)[] stages =
    {
        (2, "Stage1_Superbia", 1.2f, 0.05f, 0.04f, 0.0f, 0.30f,  10f,  25f, 1.0f, 0.92f, 0.55f), // 황금
        (3, "Stage2_Avaritia", 0.8f, 0.02f, 0.02f, 0.0f, 0.40f,  25f,  -5f, 0.7f, 0.60f, 0.10f), // 어두운 금
        (4, "Stage3_Luxuria",  1.5f, 0.08f, 0.01f, 0.03f,0.35f,  8f,   20f, 1.0f, 0.65f, 0.75f), // 분홍
        (5, "Stage4_Ira",      2.0f, 0.08f, 0.02f, 0.0f, 0.45f,  20f,  15f, 1.0f, 0.70f, 0.50f), // 화염
    };

    [MenuItem("PECCATUM/7a. Add Post-Process to Stage1 (교만)")]
    static void PPStage1() => AddPP(2, "Stage1_Superbia", 1.2f, 0.05f,0.04f,0.0f,0.30f, 10f,25f, 1.0f,0.92f,0.55f);

    [MenuItem("PECCATUM/7b. Add Post-Process to Stage2 (탐욕)")]
    static void PPStage2() => AddPP(3, "Stage2_Avaritia", 0.8f, 0.02f,0.02f,0.0f,0.40f, 25f,-5f, 0.7f,0.60f,0.10f);

    [MenuItem("PECCATUM/7c. Add Post-Process to Stage3 (색욕)")]
    static void PPStage3() => AddPP(4, "Stage3_Luxuria",  1.5f, 0.08f,0.01f,0.03f,0.35f, 8f,20f, 1.0f,0.65f,0.75f);

    [MenuItem("PECCATUM/7d. Add Post-Process to Stage4 (분노)")]
    static void PPStage4() => AddPP(5, "Stage4_Ira",      2.0f, 0.08f,0.02f,0.0f,0.45f, 20f,15f, 1.0f,0.70f,0.50f);

    static void AddPP(int buildIdx, string sceneName, float bloomI, float vR, float vG, float vB, float vI, float contrast, float sat, float fR, float fG, float fB)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/Dungeons/{sceneName}.unity", OpenSceneMode.Single);

        // 기존 Volume 삭제
        var oldVol = GameObject.Find($"PostProcess_{sceneName}");
        if (oldVol != null) Object.DestroyImmediate(oldVol);

        var volGO = new GameObject($"PostProcess_{sceneName}");
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        string profilePath = $"Assets/Scenes/Dungeons/{sceneName}_PP.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
            AssetDatabase.DeleteAsset(profilePath);
        AssetDatabase.CreateAsset(profile, profilePath);

#if UNITY_URP
        // Bloom
        var bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(bloomI);
        bloom.scatter.Override(0.75f);
        bloom.threshold.Override(0.8f);

        // Vignette
        var vig = profile.Add<Vignette>(true);
        vig.active = true;
        vig.color.Override(new Color(vR, vG, vB));
        vig.intensity.Override(vI);
        vig.smoothness.Override(0.4f);

        // Color Adjustments
        var ca = profile.Add<ColorAdjustments>(true);
        ca.active = true;
        ca.contrast.Override(contrast);
        ca.saturation.Override(sat);
        ca.colorFilter.Override(new Color(fR, fG, fB));
#endif
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[PECCATUM] PostProcess added to {sceneName} (Bloom:{bloomI}, Vignette:{vI})");
    }
}
#endif
