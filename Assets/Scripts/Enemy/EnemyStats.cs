using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 적(오크) 스탯 및 피격 임팩트 시스템
    /// 수정 사항:
    ///   - SpawnBurst: AddComponent 후 자동 Play 차단 (Stop → 설정 → Play 순서)
    ///     → "Setting the duration while system is still playing" 경고 완전 제거
    ///   - SafeSetTrigger: Animator 파라미터 존재 여부 확인 후 호출
    ///     → "Parameter does not exist" 에러 완전 제거
    /// </summary>
    public class EnemyStats : MonoBehaviour
    {
        [Header("스탯")]
        [SerializeField] private float _maxHp = 70f;

        [Header("피격 이펙트")]
        [SerializeField] private Color _hitFlashColor    = new Color(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private float _hitFlashDuration = 0.12f;

        private float                _currentHp;
        private bool                 _isDead;
        private Animator             _animator;
        private EnemyController      _controller;
        private EnemyRangedController _rangedController;
        private SpriteRenderer       _sr;
        private Color                _originalColor;
        private Coroutine            _flashCoroutine;

        private static readonly int AnimDie = Animator.StringToHash("Die");
        private static readonly int AnimHit = Animator.StringToHash("Hit");

        public bool IsDead => _isDead;

        private void Awake()
        {
            _currentHp        = _maxHp;
            _animator         = GetComponent<Animator>();
            _controller       = GetComponent<EnemyController>();
            _rangedController = GetComponent<EnemyRangedController>();
            _sr               = GetComponent<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;
        }

        // ── 안전한 Animator 트리거 ────────────────────────────────────
        /// <summary>파라미터가 존재할 때만 SetTrigger — 없으면 조용히 무시</summary>
        private void SafeSetTrigger(int hash)
        {
            if (_animator == null) return;
            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == hash)
                {
                    _animator.SetTrigger(hash);
                    return;
                }
            }
            // 파라미터 없음 → 무시 (에러 없음)
        }

        // ─────────────────────────────────────────────────────────────
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            Debug.Log($"[EnemyStats] {name} HP: {_currentHp}/{_maxHp}  (-{amount})");

            if (_currentHp <= 0f)
            {
                _isDead = true;
                if (_flashCoroutine != null)
                {
                    StopCoroutine(_flashCoroutine);
                    _flashCoroutine = null;
                    if (_sr != null) _sr.color = _originalColor;
                }
                OnDead();
            }
            else
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(HitFlash());
                SpawnHitVFX(transform.position);
                SafeSetTrigger(AnimHit);
            }
        }

        private IEnumerator HitFlash()
        {
            if (_sr != null) _sr.color = _hitFlashColor;
            yield return new WaitForSeconds(_hitFlashDuration);
            if (_sr != null && !_isDead) _sr.color = _originalColor;
            _flashCoroutine = null;
        }

        private static void SpawnHitVFX(Vector3 pos)
        {
            var root = new GameObject("EnemyHit_VFX");
            root.transform.position = pos + new Vector3(0f, 0.3f, 0f);
            SpawnBurst(root, new Color(0.9f, 0.1f, 0.1f, 1f), 12, 0.5f, 5f,  0.06f, 0.18f, 12, 1.2f);
            SpawnBurst(root, new Color(1f,   0.9f, 0.9f, 1f),  8, 0.25f, 7f, 0.04f, 0.12f, 13, 0f);
            Destroy(root, 1.0f);
        }

        private void OnDead()
        {
            Debug.Log($"[EnemyStats] {name} 사망.");
            if (_controller != null)       _controller.enabled = false;
            if (_rangedController != null) _rangedController.enabled = false;
            SafeSetTrigger(AnimDie);
            SpawnDeathVFX(transform.position);

            // 스테이지 매니저에 적 사망 통보
            StageManager.Instance?.OnEnemyDied();

            StartCoroutine(ReturnToPoolAfterDelay(1.5f));
        }

        private static void SpawnDeathVFX(Vector3 pos)
        {
            var root = new GameObject("EnemyDeath_VFX");
            root.transform.position = pos + new Vector3(0f, 0.5f, 0f);
            SpawnBurst(root, new Color(0.9f, 0.1f, 0.1f, 1f), 25, 0.8f, 7f, 0.10f, 0.30f, 12, 1.5f);
            SpawnBurst(root, new Color(1f,   0.6f, 0.1f, 1f), 15, 0.6f, 5f, 0.08f, 0.22f, 11, 0.5f);
            SpawnBurst(root, new Color(1f,   1f,   0.8f, 1f), 10, 0.3f, 9f, 0.05f, 0.15f, 13, 0f);
            Destroy(root, 2.0f);
        }

        /// <summary>
        /// 파티클 버스트 생성
        /// ★ AddComponent 직후 자동 Play 차단 → Stop → 설정 → Play 순서 보장
        ///    "Setting the duration while system is still playing" 경고 방지
        /// </summary>
        private static void SpawnBurst(GameObject parent, Color col,
            int count, float lifeMax, float speedMax,
            float sizeMin, float sizeMax, int order, float gravity)
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(parent.transform, false);

            var ps = go.AddComponent<ParticleSystem>();

            // ★ 자동 Play 즉시 차단 후 설정
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake     = false;          // ★ 자동 재생 끄기
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

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material     = new Material(Shader.Find("Sprites/Default"));
            psr.sortingOrder = order;

            // ★ 설정 완료 후 명시적 Play
            ps.Play();
        }

        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_sr != null) _sr.color = _originalColor;
            if (EnemyPool.Instance != null)
                EnemyPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }

        public void ResetStats()
        {
            _isDead    = false;
            _currentHp = _maxHp;
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            if (_sr != null) _sr.color = _originalColor;
            if (_controller != null)       _controller.enabled = true;
            if (_rangedController != null) _rangedController.enabled = true;
        }
    }
}
