using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 적(오크) 스탯 & 피격 임팩트 시스템
    ///
    /// TakeDamage 호출 시:
    ///   1. 스프라이트 빨간 플래시 (0.12초)
    ///   2. 붉은+흰 파티클 버스트 (피격 위치)
    ///   3. 짧은 넉백 임펄스
    ///   HP 0 시 → 사망 처리
    /// </summary>
    public class EnemyStats : MonoBehaviour
    {
        [Header("스탯")]
        [SerializeField] private float _maxHp = 50f;

        [Header("피격 이펙트")]
        [SerializeField] private Color _hitFlashColor   = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private float _hitFlashDuration = 0.12f;
        [SerializeField] private float _knockbackForce  = 3.5f;

        // ── 내부 ─────────────────────────────────────────────────────
        private float          _currentHp;
        private Animator       _animator;
        private EnemyController _controller;
        private SpriteRenderer  _spriteRenderer;
        private Rigidbody2D    _rb;

        private bool _isFlashing = false;
        private Color _originalColor;

        private static readonly int AnimDie  = Animator.StringToHash("Die");
        private static readonly int AnimHit  = Animator.StringToHash("Hit");

        public bool IsDead => _currentHp <= 0f;

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _currentHp      = _maxHp;
            _animator       = GetComponent<Animator>();
            _controller     = GetComponent<EnemyController>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb             = GetComponent<Rigidbody2D>();

            if (_spriteRenderer != null)
                _originalColor = _spriteRenderer.color;
        }

        // ─────────────────────────────────────────────────────────────
        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            Debug.Log($"[EnemyStats] {name} HP: {_currentHp}/{_maxHp}  (-{amount})");

            // ① 스프라이트 빨간 플래시
            if (!_isFlashing)
                StartCoroutine(HitFlash());

            // ② 피격 파티클 버스트
            SpawnHitVFX(transform.position);

            // ③ 피격 애니메이션 트리거 (컨트롤러에 Hit 파라미터 있을 때)
            if (!IsDead)
                _animator?.SetTrigger(AnimHit);

            if (IsDead)
                OnDead();
        }

        // ── 스프라이트 빨간 플래시 ────────────────────────────────────
        private IEnumerator HitFlash()
        {
            _isFlashing = true;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _hitFlashColor;

            yield return new WaitForSeconds(_hitFlashDuration);

            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;

            _isFlashing = false;
        }

        // ── 피격 파티클 VFX ──────────────────────────────────────────
        private static void SpawnHitVFX(Vector3 pos)
        {
            var root = new GameObject("EnemyHit_VFX");
            root.transform.position = pos + new Vector3(0f, 0.3f, 0f);

            // 붉은 피 튀김 버스트
            SpawnBurst(root, new Color(0.9f, 0.1f, 0.1f, 1f),
                       count: 12, lifeMax: 0.5f, speedMax: 5f,
                       sizeMin: 0.06f, sizeMax: 0.18f, order: 12, gravity: 1.2f);

            // 흰 임팩트 섬광 버스트
            SpawnBurst(root, new Color(1f, 0.9f, 0.9f, 1f),
                       count: 8, lifeMax: 0.25f, speedMax: 7f,
                       sizeMin: 0.04f, sizeMax: 0.12f, order: 13, gravity: 0f);

            Destroy(root, 1.0f);
        }

        private static void SpawnBurst(GameObject parent, Color col,
            int count, float lifeMax, float speedMax,
            float sizeMin, float sizeMax, int order, float gravity)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(parent.transform, false);

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration        = 0.2f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(lifeMax * 0.4f, lifeMax);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, speedMax);
            main.startSize       = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor      = col;
            main.maxParticles    = count + 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(gravity * 0.8f, gravity);

            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.08f;

            // 파티클 크기 감소 (소멸감)
            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material     = new Material(Shader.Find("Sprites/Default"));
            psr.sortingOrder = order;

            ps.Play();
        }

        // ── 사망 처리 ─────────────────────────────────────────────────
        private void OnDead()
        {
            Debug.Log($"[EnemyStats] {name} 사망.");
            if (_controller != null) _controller.enabled = false;
            _animator?.SetTrigger(AnimDie);

            // 사망 VFX (더 큰 폭발)
            SpawnDeathVFX(transform.position);

            StartCoroutine(ReturnToPoolAfterDelay(1.5f));
        }

        private static void SpawnDeathVFX(Vector3 pos)
        {
            var root = new GameObject("EnemyDeath_VFX");
            root.transform.position = pos + new Vector3(0f, 0.5f, 0f);

            SpawnBurst(root, new Color(0.9f, 0.1f, 0.1f, 1f),
                       count: 25, lifeMax: 0.8f, speedMax: 7f,
                       sizeMin: 0.10f, sizeMax: 0.30f, order: 12, gravity: 1.5f);

            SpawnBurst(root, new Color(1f, 0.6f, 0.1f, 1f),
                       count: 15, lifeMax: 0.6f, speedMax: 5f,
                       sizeMin: 0.08f, sizeMax: 0.22f, order: 11, gravity: 0.5f);

            SpawnBurst(root, new Color(1f, 1f, 0.8f, 1f),
                       count: 10, lifeMax: 0.3f, speedMax: 9f,
                       sizeMin: 0.05f, sizeMax: 0.15f, order: 13, gravity: 0f);

            Destroy(root, 2.0f);
        }

        // ─────────────────────────────────────────────────────────────
        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;

            if (EnemyPool.Instance != null)
                EnemyPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }

        /// <summary>풀에서 꺼낼 때 상태 초기화</summary>
        public void ResetStats()
        {
            _currentHp  = _maxHp;
            _isFlashing = false;
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;
        }
    }
}
