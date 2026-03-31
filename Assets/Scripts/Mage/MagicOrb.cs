using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 마법 구체 투사체 (96px 버전)
    /// - 런타임 생성 원형 스프라이트
    /// - 적 / 지형 충돌 → 폭발 VFX + 데미지
    /// - 최대 거리 초과 → 페이드 소멸
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class MagicOrb : MonoBehaviour
    {
        [HideInInspector] public float     damage;
        [HideInInspector] public float     speed;
        [HideInInspector] public float     maxDistance;
        [HideInInspector] public LayerMask enemyLayer;
        [HideInInspector] public Color     orbColor = new Color(0.4f, 0.8f, 1f, 1f);

        private Vector2        _dir;
        private Vector2        _startPos;
        private SpriteRenderer _sr;
        private bool           _dead;

        // ── 원형 스프라이트 캐시 ───────────────────────────────────────
        private static Sprite _circleSprite;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.2f;

            // 스프라이트 설정
            _sr.sprite       = GetCircleSprite();
            _sr.sortingOrder = 5;
        }

        public void Launch(Vector2 dir)
        {
            _dir      = dir.normalized;
            _startPos = transform.position;
            if (_sr != null) _sr.color = orbColor;
        }

        private void Update()
        {
            if (_dead) return;
            transform.position += (Vector3)(_dir * speed * Time.deltaTime);

            if (Vector2.Distance(transform.position, _startPos) >= maxDistance)
            {
                _dead = true;
                StartCoroutine(FadeOut());
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_dead) return;

            // 적 레이어
            if (enemyLayer.value != 0 &&
                (enemyLayer.value & (1 << other.gameObject.layer)) != 0)
            { Hit(other); return; }

            // 태그 폴백
            if (other.CompareTag("Enemy"))
            { Hit(other); return; }

            // 지형
            int gl = LayerMask.NameToLayer("Ground");
            int pl = LayerMask.NameToLayer("Platform");
            int l  = other.gameObject.layer;
            if (l == gl || l == pl)
            {
                _dead = true;
                SpawnVFX(transform.position, false);
                Destroy(gameObject);
            }
        }

        private void Hit(Collider2D target)
        {
            _dead = true;
            target.GetComponent<EnemyStats>()?.TakeDamage(damage);
            SpawnVFX(transform.position, true);
            Destroy(gameObject);
        }

        // ── 폭발 VFX ──────────────────────────────────────────────────
        private static void SpawnVFX(Vector3 pos, bool isHit)
        {
            var root = new GameObject("MagicOrb_VFX");
            root.transform.position = pos;

            SpawnBurst(root, new Color(0.4f, 0.85f, 1f), isHit ? 20 : 10, 0.35f, 5f, 0.08f, 0.20f, 6);
            SpawnBurst(root, new Color(0.8f, 0.4f,  1f), isHit ? 12 :  6, 0.25f, 7f, 0.05f, 0.14f, 5);

            Destroy(root, 1f);
        }

        private static void SpawnBurst(GameObject parent, Color col,
            int count, float life, float spd, float sMin, float sMax, int order)
        {
            var go = new GameObject("B");
            go.transform.SetParent(parent.transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var m = ps.main;
            m.loop           = false;
            m.duration       = 0.1f;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(1f, spd);
            m.startSize      = new ParticleSystem.MinMaxCurve(sMin, sMax);
            m.startColor     = col;
            m.maxParticles   = count + 4;
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.06f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = order;
            ps.Play();
        }

        // ── 페이드 소멸 ───────────────────────────────────────────────
        private IEnumerator FadeOut()
        {
            float t = 0f;
            Color c = _sr != null ? _sr.color : Color.white;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                if (_sr != null)
                    _sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t / 0.25f));
                yield return null;
            }
            Destroy(gameObject);
        }

        // ── 원형 스프라이트 생성 ──────────────────────────────────────
        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int size = 64;
            var tex  = new Texture2D(size, size, TextureFormat.ARGB32, false);
            float c  = size * 0.5f, r = c - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(r - d + 1f);
                float b  = Mathf.Clamp01(1f - d / r);
                tex.SetPixel(x, y, new Color(b * 0.6f + 0.4f, b * 0.8f + 0.2f, 1f, a));
            }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
            return _circleSprite;
        }
    }
}
