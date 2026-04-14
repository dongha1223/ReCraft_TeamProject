
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PeccatumFXBuilder
{
    // ── Stage1: 교만(Superbia) - 황금 먼지, 찬란한 빛 ──
    [MenuItem("PECCATUM/FX/Stage1 Add Effects (교만)")]
    public static void AddFX_Stage1()
    {
        LoadScene(2);
        AddPointLight("Light_Throne",    new Vector3(9.5f, 5.5f, -1f), new Color(1f, 0.9f, 0.3f), 8f, 12f);
        AddPointLight("Light_LeftWing",  new Vector3(3f,   5.5f, -1f), new Color(1f, 0.8f, 0.2f), 5f, 8f);
        AddPointLight("Light_RightWing", new Vector3(16f,  5.5f, -1f), new Color(1f, 0.8f, 0.2f), 5f, 8f);
        AddParticle("FX_GoldenDust", new Vector3(9.5f, 5.5f, -0.5f),
            startColor: new Color(1f, 0.85f, 0.2f, 0.6f),
            startSize: 0.08f, emissionRate: 30, speed: 0.5f, lifetime: 3f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(18f, 10f, 0.1f));
        AddDecorObject("Deco_Crown",     new Vector3(9.5f, 9f,   0f), new Color(1f, 0.9f, 0.1f));
        AddDecorObject("Deco_Pillar_L",  new Vector3(2f,   5.5f, 0f), new Color(0.9f, 0.8f, 0.3f));
        AddDecorObject("Deco_Pillar_R",  new Vector3(17f,  5.5f, 0f), new Color(0.9f, 0.8f, 0.3f));
        SaveScene(); Debug.Log("[PECCATUM] Stage1 FX added!");
    }

    // ── Stage2: 탐욕(Avaritia) - 금화 반짝임, 어두운 녹색 빛 ──
    [MenuItem("PECCATUM/FX/Stage2 Add Effects (탐욕)")]
    public static void AddFX_Stage2()
    {
        LoadScene(3);
        AddPointLight("Light_Vault",     new Vector3(9.5f, 5.5f, -1f), new Color(0.4f, 0.8f, 0.2f), 4f, 6f);
        AddPointLight("Light_Coin_A",    new Vector3(3f,   3f,   -1f), new Color(0.8f, 0.7f, 0.1f), 3f, 4f);
        AddPointLight("Light_Coin_B",    new Vector3(16f,  8f,   -1f), new Color(0.8f, 0.7f, 0.1f), 3f, 4f);
        AddParticle("FX_CoinSparks", new Vector3(9.5f, 5.5f, -0.5f),
            startColor: new Color(0.9f, 0.75f, 0.1f, 0.8f),
            startSize: 0.05f, emissionRate: 15, speed: 0.3f, lifetime: 2f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(18f, 10f, 0.1f));
        AddParticle("FX_DarkMist", new Vector3(9.5f, 2f, -0.3f),
            startColor: new Color(0.1f, 0.15f, 0.05f, 0.4f),
            startSize: 0.5f, emissionRate: 8, speed: 0.1f, lifetime: 5f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(20f, 2f, 0.1f));
        SaveScene(); Debug.Log("[PECCATUM] Stage2 FX added!");
    }

    // ── Stage3: 색욕(Luxuria) - 장미 꽃잎, 붉은 안개 ──
    [MenuItem("PECCATUM/FX/Stage3 Add Effects (색욕)")]
    public static void AddFX_Stage3()
    {
        LoadScene(4);
        AddPointLight("Light_Garden_C",  new Vector3(9.5f, 5.5f, -1f), new Color(1f, 0.3f, 0.5f), 6f, 10f);
        AddPointLight("Light_Rose_L",    new Vector3(3f,   5.5f, -1f), new Color(0.9f, 0.2f, 0.4f), 3f, 5f);
        AddPointLight("Light_Rose_R",    new Vector3(16f,  5.5f, -1f), new Color(0.9f, 0.2f, 0.4f), 3f, 5f);
        AddParticle("FX_RosePetals", new Vector3(9.5f, 8f, -0.5f),
            startColor: new Color(0.9f, 0.2f, 0.3f, 0.7f),
            startSize: 0.12f, emissionRate: 20, speed: 0.4f, lifetime: 4f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(18f, 1f, 0.1f));
        AddParticle("FX_PinkMist", new Vector3(9.5f, 5.5f, -0.2f),
            startColor: new Color(0.8f, 0.2f, 0.4f, 0.2f),
            startSize: 0.8f, emissionRate: 5, speed: 0.05f, lifetime: 6f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(20f, 10f, 0.1f));
        SaveScene(); Debug.Log("[PECCATUM] Stage3 FX added!");
    }

    // ── Stage4: 분노(Ira) - 화염 파티클, 붉은 용암 빛 ──
    [MenuItem("PECCATUM/FX/Stage4 Add Effects (분노)")]
    public static void AddFX_Stage4()
    {
        LoadScene(5);
        AddPointLight("Light_Lava_C",    new Vector3(9.5f, 5.5f, -1f), new Color(1f, 0.3f, 0.05f), 6f, 10f);
        AddPointLight("Light_Ember_L",   new Vector3(3f,   5.5f, -1f), new Color(1f, 0.2f, 0.0f),  4f, 6f);
        AddPointLight("Light_Ember_R",   new Vector3(16f,  5.5f, -1f), new Color(1f, 0.2f, 0.0f),  4f, 6f);
        AddPointLight("Light_Floor",     new Vector3(9.5f, 1f,   -1f), new Color(1f, 0.5f, 0.0f),  8f, 3f);
        AddParticle("FX_Embers", new Vector3(9.5f, 1f, -0.5f),
            startColor: new Color(1f, 0.4f, 0.05f, 0.9f),
            startSize: 0.06f, emissionRate: 40, speed: 1.5f, lifetime: 2f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(20f, 0.5f, 0.1f));
        AddParticle("FX_Smoke", new Vector3(9.5f, 2f, -0.3f),
            startColor: new Color(0.15f, 0.05f, 0.02f, 0.5f),
            startSize: 0.6f, emissionRate: 10, speed: 0.3f, lifetime: 4f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(18f, 2f, 0.1f));
        AddParticle("FX_FireSparks", new Vector3(9.5f, 5.5f, -0.4f),
            startColor: new Color(1f, 0.6f, 0.1f, 0.8f),
            startSize: 0.04f, emissionRate: 25, speed: 2f, lifetime: 1.5f,
            shape: ParticleSystemShapeType.Box, shapeScale: new Vector3(16f, 8f, 0.1f));
        SaveScene(); Debug.Log("[PECCATUM] Stage4 FX added!");
    }

    // ── Helpers ──
    static void LoadScene(int buildIndex)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    static void SaveScene()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
    }

    static void AddPointLight(string name, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    static void AddParticle(string name, Vector3 pos, Color startColor, float startSize,
        float emissionRate, float speed, float lifetime,
        ParticleSystemShapeType shape, Vector3 shapeScale)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor = startColor;
        main.startSize = startSize;
        main.startSpeed = speed;
        main.startLifetime = lifetime;
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        var shapeModule = ps.shape;
        shapeModule.enabled = true;
        shapeModule.shapeType = shape;
        shapeModule.scale = shapeScale;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 5;
    }

    static void AddDecorObject(string name, Vector3 pos, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        renderer.material = mat;
        var col = go.GetComponent<MeshCollider>();
        if (col) Object.DestroyImmediate(col);
    }
}
#endif
