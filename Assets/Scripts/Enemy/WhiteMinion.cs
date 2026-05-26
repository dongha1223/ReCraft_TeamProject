using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 하얀색 하수인.
    /// 스폰 후 대기 → 정면으로 돌진 → 최대 거리 도달 또는 플레이어 접촉 시 폭발.
    /// 플레이어가 대쉬 중이면 접촉 시 통과 (폭발하지 않음).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class WhiteMinion : MinionBase
    {
        [Header("돌진")]
        [Tooltip("돌진 속도 (m/s)")]
        [SerializeField] private float _dashSpeed       = 8f;
        [Tooltip("이 거리 이상 이동하면 폭발")]
        [SerializeField] private float _maxDashDistance = 10f;
        [Tooltip("돌진 전 대기 시간 (초) — 전조 이펙트와 함께 플레이어가 피할 시간")]
        [SerializeField] private float _windupDuration  = 0.8f;

        [Header("폭발")]
        [Tooltip("폭발 반경 (m)")]
        [SerializeField] private float _explosionRadius  = 2f;
        [Tooltip("폭발 데미지")]
        [SerializeField] private float _explosionDamage  = 20f;
        [Tooltip("폭발 넉백 강도")]
        [SerializeField] private float _explosionKnockback = 6f;
        [Tooltip("폭발 데미지 감지 레이어 (플레이어 레이어)")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("이펙트")]
        [Tooltip("돌진 전조 이펙트 프리팹")]
        [SerializeField] private GameObject _windupEffectPrefab;
        [Tooltip("폭발 이펙트 프리팹")]
        [SerializeField] private GameObject _explosionEffectPrefab;

        private Rigidbody2D _rb;
        private bool        _isDashing;
        private bool        _hasExploded;
        private Vector2     _dashStartPos;

        // ── 생명주기 ──────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody2D>();
        }

        // ── MinionBase 구현 ───────────────────────────────────────────────

        protected override IEnumerator AttackCoroutine()
        {
            // ① 전조 이펙트 표시
            if (_windupEffectPrefab != null)
                Instantiate(_windupEffectPrefab, transform.position, Quaternion.identity);

            yield return new WaitForSeconds(_windupDuration);

            if (_stats.IsDead) yield break;

            // ② 돌진 시작
            _dashStartPos = transform.position;
            _isDashing    = true;
            _rb.linearVelocity = new Vector2(_facingDir * _dashSpeed, 0f);

            // ③ 최대 거리 도달 체크 (플레이어 접촉 폭발은 OnTriggerEnter2D에서 처리)
            while (_isDashing && !_stats.IsDead)
            {
                float traveled = Vector2.Distance(_dashStartPos, transform.position);
                if (traveled >= _maxDashDistance)
                {
                    Explode();
                    yield break;
                }
                yield return null;
            }
        }

        protected override void OnForceCleanup()
        {
            _isDashing = false;
            _rb.linearVelocity = Vector2.zero;
        }

        // ── 충돌 처리 ─────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!_isDashing || _hasExploded) return;
            if (!col.CompareTag("Player"))   return;

            // 대쉬 중인 플레이어는 통과 가능
            var playerDash = col.GetComponentInParent<PlayerDash>();
            if (playerDash != null && playerDash.IsDashing) return;

            Explode();
        }

        // ── 폭발 ──────────────────────────────────────────────────────────

        private void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;
            _isDashing   = false;
            _rb.linearVelocity = Vector2.zero;

            // 폭발 이펙트 재생
            if (_explosionEffectPrefab != null)
                Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);

            // 범위 내 플레이어에게 데미지
            ApplyExplosionDamage();

            Destroy(gameObject);
        }

        private void ApplyExplosionDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _playerLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;
                if (damageable.IsInvincible) continue;

                damageable.TakeDamage(new HitInfo
                {
                    Damage         = _explosionDamage,
                    DamageType     = DamageType.Physical,
                    SourcePosition = transform.position,
                    KnockbackForce = _explosionKnockback,
                });
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 폭발 반경
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
            // 최대 돌진 거리
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.3f);
            Vector3 end = transform.position + Vector3.right * _facingDir * _maxDashDistance;
            Gizmos.DrawLine(transform.position, end);
            Gizmos.DrawWireSphere(end, 0.15f);
        }
#endif
    }
}
