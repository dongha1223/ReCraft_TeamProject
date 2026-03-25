using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검기 발산 투사체
    /// - 흰색 코어 + 노란 글로우 2중 레이어 초승달 메시
    /// - LayerMask + 태그 "Enemy" 이중 감지
    /// - 사망한 적(IsDead) 무시 → 투사체 통과
    /// - Dissolve 중복 호출 방지
    /// - SpawnBurst: Stop → 설정 → Play 순서로 파티클 경고 완전 제거
    /// - SkillObjectPool 기반 풀링: Destroy 대신 풀 반환
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SwordEnergyProjectile : MonoBehaviour
    {
        [HideInInspector] public float     damage      = 30f;
        [HideInInspector] public float     speed       = 10f;
        [HideInInspector] public float     maxDistance = 14f;
        [HideInInspector] public LayerMask enemyLayer;
        [HideInInspector] public bool      facingRight = true;

        private Vector2          _dir;
        private float            _traveled;
        private bool             _active = true;
        private MeshFilter       _mf;
        private MeshRenderer     _mr;
        private MeshRenderer     _glowMr;
        private CircleCollider2D _col;
        private GameObject       _glowGO;

        // 오브젝트마다 1개씩 보유하는 인스턴스 재질
        private Material _coreMat;
        private Material _glowMat;

        private readonly HashSet<Collider2D> _hit = new HashSet<Collider2D>();

        // ── 공유 메시 캐시 (방향당 1개, 최초 1회만 빌드) ─────────────────
        private static Mesh _meshRight;
        private static Mesh _meshLeft;
        private static Mesh _glowMeshRight;
        private static Mesh _glowMeshLeft;

        private const float _outerR   = 0.52f;
        private const float _innerR   = 0.50f;
        private const float _offset   = 0.82f;
        private const int   _segments = 36;

        // ═════════════════════════════════════════════════════════════════
        private void Awake()
        {
            _mf  = GetComponent<MeshFilter>();
            _mr  = GetComponent<MeshRenderer>();
            _col = gameObject.AddComponent<CircleCollider2D>();
            _col.radius    = 0.42f;
            _col.isTrigger = true;
        }

        /// <summary>
        /// 풀에서 꺼낼 때 호출 — 방향 설정 + 내부 상태 초기화
        /// </summary>
        public void Setup(bool fr)
        {
            facingRight = fr;
            _dir        = Vector2.zero;
            _traveled   = 0f;
            _active     = true;
            _hit.Clear();

            // 코어 메시 할당 (static 캐시 사용)
            _mf.mesh = GetOrBuildMesh(facingRight, false);

            // 코어 재질: 최초 1회 생성, 이후 색상만 재설정
            if (_coreMat == null)
            {
                _coreMat              = new Material(Shader.Find("Sprites/Default"));
                _mr.sharedMaterial    = _coreMat;
                _mr.sortingOrder      = 7;
            }
            _coreMat.color = Color.white;
            _mr.enabled    = true;

            // 글로우 자식: 최초 1회 생성, 이후 재사용
            if (_glowGO == null)
            {
                _glowGO = new GameObject("Glow");
                _glowGO.transform.SetParent(transform, false);
                _glowGO.transform.localScale = Vector3.one * 1.40f;

                _glowGO.AddComponent<MeshFilter>().mesh = GetOrBuildMesh(facingRight, true);
                _glowMr                    = _glowGO.AddComponent<MeshRenderer>();
                _glowMat                   = new Material(Shader.Find("Sprites/Default"));
                _glowMr.sharedMaterial     = _glowMat;
                _glowMr.sortingOrder       = 6;
            }
            else
            {
                // 방향이 바뀌었을 수 있으므로 메시 갱신
                _glowGO.GetComponent<MeshFilter>().mesh = GetOrBuildMesh(facingRight, true);
            }

            _glowMat.color  = new Color(1f, 0.88f, 0.08f, 0.60f);
            _glowMr.enabled = true;

            if (_col != null) _col.enabled = true;
        }

        public void Launch(Vector2 direction) => _dir = direction.normalized;

        private void Update()
        {
            if (!_active) return;

            float step = speed * Time.deltaTime;
            transform.Translate(_dir * step, Space.World);
            _traveled += step;

            if (TryHitEnemy((Vector2)transform.position) || _traveled >= maxDistance)
                Dissolve();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active) return;
            if (_hit.Contains(other)) return;
            var stats = other.GetComponent<EnemyStats>();
            if (stats == null || stats.IsDead) return;

            _hit.Add(other);
            stats.TakeDamage(damage);
            SpawnImpactVFX(transform.position);
            Dissolve();
        }

        private bool TryHitEnemy(Vector2 pos)
        {
            // 1차: LayerMask
            if (enemyLayer.value != 0)
            {
                Collider2D col = Physics2D.OverlapCircle(pos, 0.44f, enemyLayer);
                if (col != null && !_hit.Contains(col))
                {
                    var stats = col.GetComponent<EnemyStats>();
                    if (stats != null && !stats.IsDead)
                    {
                        _hit.Add(col);
                        stats.TakeDamage(damage);
                        SpawnImpactVFX(pos);
                        return true;
                    }
                }
            }

            // 2차: 태그 "Enemy" 폴백
            foreach (var c in Physics2D.OverlapCircleAll(pos, 0.44f))
            {
                if (_hit.Contains(c)) continue;
                if (!c.CompareTag("Enemy")) continue;
                var s = c.GetComponent<EnemyStats>();
                if (s == null || s.IsDead) continue;
                _hit.Add(c);
                s.TakeDamage(damage);
                SpawnImpactVFX(pos);
                return true;
            }
            return false;
        }

        private static void SpawnImpactVFX(Vector3 pos)
        {
            var root = new GameObject("SwordImpact_VFX");
            root.transform.position = pos;
            SpawnBurst(root, Color.white,                       18, 0.35f, 6f, 0.05f, 0.14f, 11);
            SpawnBurst(root, new Color(1f, 0.88f, 0.1f, 0.9f), 22, 0.45f, 9f, 0.08f, 0.20f, 10);
            Destroy(root, 1.0f);
        }

        /// <summary>
        /// ★ Stop → 설정 → Play 순서로 파티클 경고 완전 제거
        /// </summary>
        private static void SpawnBurst(GameObject parent, Color col,
            int count, float lifeMax, float speedMax,
            float sizeMin, float sizeMax, int order)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(parent.transform, false);

            var ps = go.AddComponent<ParticleSystem>();

            // ★ 자동 Play 즉시 차단
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake     = false;
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

            // ★ 설정 완료 후 명시적 Play
            ps.Play();
        }

        private void Dissolve()
        {
            if (!_active) return;
            _active = false;

            // 렌더러/콜라이더 즉시 숨김
            if (_mr     != null) _mr.enabled     = false;
            if (_glowMr != null) _glowMr.enabled = false;
            if (_col    != null) _col.enabled    = false;

            // 1프레임 후 풀로 반환 (현재 프레임 Update/Trigger 안전 완료 보장)
            StartCoroutine(ReturnToPool());
        }

        private IEnumerator ReturnToPool()
        {
            yield return null;
            if (SkillObjectPool.Instance != null)
                SkillObjectPool.Instance.ReturnProjectile(this);
            else
                Destroy(gameObject);
        }

        // ── Static 메시 캐시 ──────────────────────────────────────────────
        private static Mesh GetOrBuildMesh(bool fr, bool isGlow)
        {
            const float glowOffset = _offset * 0.90f;

            if (isGlow)
            {
                if (fr)
                {
                    _glowMeshRight ??= ThinCrescentMesh.Build(_outerR, _innerR, glowOffset, _segments, true);
                    return _glowMeshRight;
                }
                else
                {
                    _glowMeshLeft ??= ThinCrescentMesh.Build(_outerR, _innerR, glowOffset, _segments, false);
                    return _glowMeshLeft;
                }
            }
            else
            {
                if (fr)
                {
                    _meshRight ??= ThinCrescentMesh.Build(_outerR, _innerR, _offset, _segments, true);
                    return _meshRight;
                }
                else
                {
                    _meshLeft ??= ThinCrescentMesh.Build(_outerR, _innerR, _offset, _segments, false);
                    return _meshLeft;
                }
            }
        }
    }

    // ── ThinCrescentMesh ──────────────────────────────────────────────────
    public static class ThinCrescentMesh
    {
        public static Mesh Build(float outerRadius, float innerRadius,
                                  float offset, int segments, bool facingRight)
        {
            float R = outerRadius, r = innerRadius, d = offset;
            d = Mathf.Clamp(d, Mathf.Abs(R - r) + 0.001f, R + r - 0.001f);

            float tipX     = (R * R - r * r + d * d) / (2f * d);
            float tipY     = Mathf.Sqrt(Mathf.Max(0f, R * R - tipX * tipX));
            float outerBot = Mathf.Atan2(-tipY, tipX);
            float outerTop = Mathf.Atan2( tipY, tipX);
            float itx      = tipX - d;
            float innerTop = Mathf.Atan2( tipY, itx);
            float innerBot = Mathf.Atan2(-tipY, itx);
            float sweep    = innerBot - innerTop;
            if (sweep < 0f) sweep += 2f * Mathf.PI;

            int n = segments + 1;
            var verts = new Vector3[2 * n];
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(outerBot, outerTop, (float)i / segments);
                float x = R * Mathf.Cos(a), y = R * Mathf.Sin(a);
                verts[i] = new Vector3(facingRight ? x : -x, y, 0f);
            }
            for (int i = 0; i < n; i++)
            {
                float a = innerTop + sweep * ((float)i / segments);
                float x = d + r * Mathf.Cos(a), y = r * Mathf.Sin(a);
                verts[n + i] = new Vector3(facingRight ? x : -x, y, 0f);
            }

            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int oa = i, ob = i + 1;
                int ic = n + (segments - i), id = n + (segments - i - 1);
                int j  = i * 6;
                tris[j]   = oa; tris[j+1] = ob; tris[j+2] = ic;
                tris[j+3] = ob; tris[j+4] = id; tris[j+5] = ic;
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
