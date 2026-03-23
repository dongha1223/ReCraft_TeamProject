using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검기 발산 투사체 — 불꽃이 타오르는 초승달 형태
    ///
    /// 형태:
    ///   facingRight=true  → 🌓 (볼록 오른쪽, 이동 방향: 오른쪽)
    ///   facingRight=false → 🌗 (볼록 왼쪽,  이동 방향: 왼쪽)
    ///
    /// 레이어:
    ///   코어  : 밝은 주황/노랑 초승달 메시
    ///   글로우 : 짙은 오렌지/빨강, 1.45× 크기로 외곽 빛남
    ///   불꽃  : 초승달 외곽에서 위로 흩날리는 파티클 (주황→노랑→흰색)
    ///   스파크 : 빠르게 튀는 밝은 불씨 파티클
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SwordEnergyProjectile : MonoBehaviour
    {
        // ── PlayerSkill 에서 주입 ─────────────────────────────────────
        [HideInInspector] public float     damage      = 30f;
        [HideInInspector] public float     speed       = 10f;
        [HideInInspector] public float     maxDistance = 14f;
        [HideInInspector] public LayerMask enemyLayer;
        [HideInInspector] public bool      facingRight = true;

        // ── 내부 ─────────────────────────────────────────────────────
        private Vector2          _dir;
        private float            _traveled;
        private bool             _active = true;
        private MeshRenderer     _mr;
        private MeshRenderer     _glowMr;
        private ParticleSystem   _fireParts;
        private ParticleSystem   _sparkParts;

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            const float outerR   = 0.52f;
            const float innerR   = 0.50f;
            const float offset   = 0.82f;
            const int   segments = 36;

            // ── 코어 레이어 (밝은 주황/노랑) ──────────────────────────
            var mf = GetComponent<MeshFilter>();
            mf.mesh = ThinCrescentMesh.Build(outerR, innerR, offset, segments, facingRight);

            var matCore = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 0.70f, 0.10f, 1f)   // 밝은 주황
            };
            _mr.material     = matCore;
            _mr.sortingOrder = 7;

            // ── 글로우 레이어 (짙은 오렌지/빨강) ──────────────────────
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(transform, false);
            glowGO.transform.localPosition = Vector3.zero;
            glowGO.transform.localScale    = Vector3.one * 1.45f;

            var glowMf = glowGO.AddComponent<MeshFilter>();
            _glowMr    = glowGO.AddComponent<MeshRenderer>();

            glowMf.mesh = ThinCrescentMesh.Build(outerR, innerR, offset * 0.88f, segments, facingRight);

            var matGlow = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 0.22f, 0.02f, 0.65f)   // 짙은 오렌지-레드
            };
            _glowMr.material     = matGlow;
            _glowMr.sortingOrder = 6;

            // ── 파티클 생성 ───────────────────────────────────────────
            _fireParts  = BuildFireParticles();
            _sparkParts = BuildSparkParticles();
        }

        // ─────────────────────────────────────────────────────────────
        //  불꽃 파티클 — 초승달 외곽에서 위로 흩날리는 불꽃
        // ─────────────────────────────────────────────────────────────
        private ParticleSystem BuildFireParticles()
        {
            var go = new GameObject("Fire_Particles");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration        = 99f;           // 투사체 살아있는 동안 계속 방출
            main.loop            = true;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 1.8f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startColor      = new Color(1f, 0.65f, 0.10f, 1f);
            main.maxParticles    = 150;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.5f, -1.4f);   // 위로 상승

            var em = ps.emission;
            em.rateOverTime = 60f;

            // 초승달 크기에 맞춘 원형 방출
            var sh = ps.shape;
            sh.shapeType       = ParticleSystemShapeType.Circle;
            sh.radius          = 0.40f;
            sh.radiusThickness = 1f;

            // 색상 변화: 짙은 주황 → 노랑 → 흰색 투명
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.00f),
                    new GradientColorKey(new Color(1f, 0.75f, 0.15f), 0.35f),
                    new GradientColorKey(new Color(1f, 1.00f, 0.70f), 0.70f),
                    new GradientColorKey(new Color(1f, 1.00f, 1.00f), 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.00f),
                    new GradientAlphaKey(0.9f, 0.40f),
                    new GradientAlphaKey(0.0f, 1.00f),
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // 크기 감소 (생성 → 소멸)
            var sizeOL   = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size  = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0.0f, 1.0f),
                    new Keyframe(0.6f, 0.55f),
                    new Keyframe(1.0f, 0.0f)
                ));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material     = new Material(Shader.Find("Sprites/Default"));
            psr.sortingOrder = 8;

            ps.Play();
            return ps;
        }

        // ─────────────────────────────────────────────────────────────
        //  스파크 파티클 — 빠르게 튀어나가는 밝은 불씨
        // ─────────────────────────────────────────────────────────────
        private ParticleSystem BuildSparkParticles()
        {
            var go = new GameObject("Spark_Particles");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration        = 99f;
            main.loop            = true;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.8f, 5.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
            main.startColor      = new Color(1f, 0.95f, 0.55f, 1f);
            main.maxParticles    = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f, 0.5f);

            var em = ps.emission;
            em.rateOverTime = 22f;

            var sh = ps.shape;
            sh.shapeType       = ParticleSystemShapeType.Circle;
            sh.radius          = 0.40f;
            sh.radiusThickness = 1f;

            // 색상: 흰/노랑 → 주황 → 투명
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 1.0f, 0.90f), 0.00f),
                    new GradientColorKey(new Color(1f, 0.7f, 0.20f), 0.45f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 0.0f), 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.00f),
                    new GradientAlphaKey(0.7f, 0.50f),
                    new GradientAlphaKey(0.0f, 1.00f),
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material     = new Material(Shader.Find("Sprites/Default"));
            psr.sortingOrder = 9;

            ps.Play();
            return ps;
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>발사 방향 설정</summary>
        public void Launch(Vector2 direction)
        {
            _dir = direction.normalized;
        }

        // ─────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_active) return;

            float step = speed * Time.deltaTime;
            transform.Translate(_dir * step, Space.World);
            _traveled += step;

            // 적 감지
            Collider2D hit = Physics2D.OverlapCircle(
                (Vector2)transform.position, 0.44f, enemyLayer);

            if (hit != null && hit.TryGetComponent(out EnemyStats enemy))
            {
                enemy.TakeDamage(damage);
                SpawnImpactVFX(transform.position);
                Dissolve();
                return;
            }

            if (_traveled >= maxDistance)
                Dissolve();
        }

        // ── 충돌 임팩트 파티클 (불꽃 색 적용) ────────────────────────
        private static void SpawnImpactVFX(Vector3 pos)
        {
            var root = new GameObject("SwordImpact_VFX");
            root.transform.position = pos;

            // 주황 섬광 버스트
            SpawnBurst(root, new Color(1f, 0.70f, 0.10f, 1.0f),
                       20, 0.35f, 7f,  0.05f, 0.16f, 11);
            // 짙은 오렌지 글로우 버스트
            SpawnBurst(root, new Color(1f, 0.25f, 0.05f, 0.9f),
                       25, 0.50f, 10f, 0.08f, 0.22f, 10);
            // 밝은 흰/노랑 스파크 버스트
            SpawnBurst(root, new Color(1f, 1.00f, 0.80f, 1.0f),
                       14, 0.25f, 5f,  0.03f, 0.10f, 12);

            Destroy(root, 1.0f);
        }

        private static void SpawnBurst(GameObject parent, Color col,
            int count, float lifeMax, float speedMax,
            float sizeMin, float sizeMax, int order)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(parent.transform, false);

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration        = 0.3f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(lifeMax * 0.4f, lifeMax);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, speedMax);
            main.startSize       = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor      = col;
            main.maxParticles    = count + 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0f, 0.3f);

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.1f;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material     = new Material(Shader.Find("Sprites/Default"));
            psr.sortingOrder = order;

            ps.Play();
        }

        private void Dissolve()
        {
            _active = false;
            if (_mr     != null) _mr.enabled     = false;
            if (_glowMr != null) _glowMr.enabled = false;

            // 파티클은 새 파티클 방출 중단 후 남은 파티클이 자연스럽게 소멸
            if (_fireParts  != null) _fireParts.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_sparkParts != null) _sparkParts.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            Destroy(gameObject, 0.6f);   // 파티클 fade-out 시간 확보
        }
    }

    // =====================================================================
    //  ThinCrescentMesh  —  사진과 같은 얇고 뾰족한 실초승달 메시 빌더
    //
    //  수학 원리:
    //    외원(R, 원점)과 오른쪽으로 offset 이동된 내원(r, (d,0)) 의 교차 영역을
    //    제거한 나머지 = 초승달
    //
    //    R ≈ r,  d 크면 → 얇고 긴 초승달 (사진과 같은 실초승달)
    //    R ≈ r,  d 작으면 → 두꺼운 반달 (D/C 자)
    //
    //  facingRight=true  → 볼록 오른쪽 (🌓, 오른쪽 이동)
    //  facingRight=false → 볼록 왼쪽  (🌗, 왼쪽 이동)
    // =====================================================================
    public static class ThinCrescentMesh
    {
        public static Mesh Build(float outerRadius, float innerRadius,
                                  float offset,      int   segments,
                                  bool  facingRight)
        {
            float R = outerRadius, r = innerRadius, d = offset;

            // ── ① 두 원의 교점(초승달 끝 꼭짓점) ─────────────────────
            d = Mathf.Clamp(d, Mathf.Abs(R - r) + 0.001f, R + r - 0.001f);

            float tipX = (R * R - r * r + d * d) / (2f * d);
            float tipY = Mathf.Sqrt(Mathf.Max(0f, R * R - tipX * tipX));

            // ── ② 외원 호 각도 범위 ───────────────────────────────────
            float outerBot = Mathf.Atan2(-tipY,  tipX);
            float outerTop = Mathf.Atan2( tipY,  tipX);

            // ── ③ 내원 호 각도 범위 ───────────────────────────────────
            float itx      = tipX - d;
            float innerTop = Mathf.Atan2( tipY, itx);
            float innerBot = Mathf.Atan2(-tipY, itx);

            float sweep = innerBot - innerTop;
            if (sweep < 0f) sweep += 2f * Mathf.PI;

            // ── ④ 버텍스 배열 ────────────────────────────────────────
            int   n     = segments + 1;
            var   verts = new Vector3[2 * n];

            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(outerBot, outerTop, (float)i / segments);
                float x = R * Mathf.Cos(a);
                float y = R * Mathf.Sin(a);
                verts[i] = new Vector3(facingRight ? x : -x, y, 0f);
            }

            for (int i = 0; i < n; i++)
            {
                float a = innerTop + sweep * ((float)i / segments);
                float x = d + r * Mathf.Cos(a);
                float y =     r * Mathf.Sin(a);
                verts[n + i] = new Vector3(facingRight ? x : -x, y, 0f);
            }

            // ── ⑤ 트라이앵글 ────────────────────────────────────────
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int oa = i,                  ob = i + 1;
                int ic = n + (segments - i), id = n + (segments - i - 1);

                int j = i * 6;
                tris[j    ] = oa; tris[j + 1] = ob; tris[j + 2] = ic;
                tris[j + 3] = ob; tris[j + 4] = id; tris[j + 5] = ic;
            }

            var mesh = new Mesh
            {
                name = facingRight ? "ThinCrescent_Right(🌓)" : "ThinCrescent_Left(🌗)"
            };
            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
