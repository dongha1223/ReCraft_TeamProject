using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검기 발산 투사체
    /// 얇고 우아한 실초승달(ThinCrescent) 메시 사용
    /// 흰색 코어 + 노란 글로우 2중 레이어
    ///
    /// 피격 감지: CircleCollider2D (Trigger) + OverlapCircle 이중 방식
    /// → 레이어 미설정 시에도 태그("Enemy")로 폴백 감지
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
        private Vector2      _dir;
        private float        _traveled;
        private bool         _active = true;
        private MeshRenderer _mr;
        private MeshRenderer _glowMr;
        private CircleCollider2D _col;

        // 이미 피격한 적 추적 (중복 방지)
        private readonly HashSet<Collider2D> _hit = new HashSet<Collider2D>();

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();

            // 트리거 콜라이더 추가 (확실한 감지)
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.radius    = 0.42f;
            _col.isTrigger = true;
        }

        private void Start()
        {
            const float outerR   = 0.52f;
            const float innerR   = 0.50f;
            const float offset   = 0.82f;
            const int   segments = 36;

            // ── 코어 레이어 (순백) ────────────────────────────────────
            var mf   = GetComponent<MeshFilter>();
            mf.mesh  = ThinCrescentMesh.Build(outerR, innerR, offset, segments, facingRight);

            var matCore = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
            _mr.material     = matCore;
            _mr.sortingOrder = 7;

            // ── 글로우 레이어 (노란색, 1.4×) ─────────────────────────
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(transform, false);
            glowGO.transform.localPosition = Vector3.zero;
            glowGO.transform.localScale    = Vector3.one * 1.40f;

            var glowMf = glowGO.AddComponent<MeshFilter>();
            _glowMr    = glowGO.AddComponent<MeshRenderer>();

            glowMf.mesh = ThinCrescentMesh.Build(outerR, innerR, offset * 0.90f, segments, facingRight);

            var matGlow = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 0.88f, 0.08f, 0.60f)
            };
            _glowMr.material     = matGlow;
            _glowMr.sortingOrder = 6;
        }

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

            // ── 이중 감지: OverlapCircle (레이어 기반) ────────────────
            Vector2 pos = transform.position;
            bool hit = false;

            // 1차: layerMask 기반 OverlapCircle
            if (enemyLayer.value != 0)
            {
                Collider2D col = Physics2D.OverlapCircle(pos, 0.44f, enemyLayer);
                if (col != null && !_hit.Contains(col))
                {
                    _hit.Add(col);
                    col.GetComponent<EnemyStats>()?.TakeDamage(damage);
                    SpawnImpactVFX(pos);
                    hit = true;
                }
            }

            // 2차 폴백: 태그 "Enemy"로 감지
            if (!hit)
            {
                Collider2D[] cols = Physics2D.OverlapCircleAll(pos, 0.44f);
                foreach (var c in cols)
                {
                    if (_hit.Contains(c)) continue;
                    if (!c.CompareTag("Enemy")) continue;
                    _hit.Add(c);
                    c.GetComponent<EnemyStats>()?.TakeDamage(damage);
                    SpawnImpactVFX(pos);
                    hit = true;
                    break;
                }
            }

            if (hit || _traveled >= maxDistance)
                Dissolve();
        }

        // ── 트리거 콜라이더 방식 (보조) ──────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active) return;
            if (_hit.Contains(other)) return;

            var stats = other.GetComponent<EnemyStats>();
            if (stats == null) return;

            _hit.Add(other);
            stats.TakeDamage(damage);
            SpawnImpactVFX(transform.position);
            Dissolve();
        }

        // ── 충돌 임팩트 파티클 ────────────────────────────────────────
        private static void SpawnImpactVFX(Vector3 pos)
        {
            var root = new GameObject("SwordImpact_VFX");
            root.transform.position = pos;

            SpawnBurst(root, Color.white,
                       18, 0.35f, 6f, 0.05f, 0.14f, 11);
            SpawnBurst(root, new Color(1f, 0.88f, 0.1f, 0.9f),
                       22, 0.45f, 9f, 0.08f, 0.20f, 10);

            Destroy(root, 1.0f);
        }

        private static void SpawnBurst(GameObject parent, Color col,
            int count, float lifeMax, float speedMax,
            float sizeMin, float sizeMax, int order)
        {
            var go  = new GameObject("Burst");
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
            if (_mr    != null) _mr.enabled    = false;
            if (_glowMr != null) _glowMr.enabled = false;
            if (_col    != null) _col.enabled   = false;
            Destroy(gameObject, 0.05f);
        }
    }

    // =====================================================================
    //  ThinCrescentMesh — 얇고 뾰족한 실초승달 메시 빌더
    //  facingRight=true  → 🌓 (볼록 오른쪽)
    //  facingRight=false → 🌗 (볼록 왼쪽)
    // =====================================================================
    public static class ThinCrescentMesh
    {
        public static Mesh Build(float outerRadius, float innerRadius,
                                  float offset,      int   segments,
                                  bool  facingRight)
        {
            float R = outerRadius, r = innerRadius, d = offset;
            d = Mathf.Clamp(d, Mathf.Abs(R - r) + 0.001f, R + r - 0.001f);

            float tipX = (R * R - r * r + d * d) / (2f * d);
            float tipY = Mathf.Sqrt(Mathf.Max(0f, R * R - tipX * tipX));

            float outerBot = Mathf.Atan2(-tipY,  tipX);
            float outerTop = Mathf.Atan2( tipY,  tipX);

            float itx      = tipX - d;
            float innerTop = Mathf.Atan2( tipY, itx);
            float innerBot = Mathf.Atan2(-tipY, itx);

            float sweep = innerBot - innerTop;
            if (sweep < 0f) sweep += 2f * Mathf.PI;

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

            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int oa = i,           ob = i + 1;
                int ic = n + (segments - i);
                int id = n + (segments - i - 1);
                int j  = i * 6;
                tris[j]     = oa; tris[j + 1] = ob; tris[j + 2] = ic;
                tris[j + 3] = ob; tris[j + 4] = id; tris[j + 5] = ic;
            }

            var mesh = new Mesh
            {
                name      = facingRight ? "ThinCrescent_Right" : "ThinCrescent_Left",
                vertices  = verts,
                triangles = tris
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
