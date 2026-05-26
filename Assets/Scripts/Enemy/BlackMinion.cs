using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 검은색 하수인.
    /// 스폰 후 일정 시간 대기 → 레이저 전조 → 레이저 데미지 판정 → 소멸.
    /// 레이저는 정면(facingDir) 방향으로 BoxCast2D를 사용해 플레이어를 감지한다.
    /// </summary>
    public class BlackMinion : MinionBase
    {
        [Header("레이저")]
        [Tooltip("레이저 전조 대기 시간 (초) — 이 시간 동안 전조 이펙트가 표시됨")]
        [SerializeField] private float _laserWindupDuration = 1.2f;
        [Tooltip("레이저 이펙트 지속 시간 (초)")]
        [SerializeField] private float _laserDuration       = 0.6f;
        [Tooltip("레이저 최대 사거리 (m)")]
        [SerializeField] private float _laserLength         = 12f;
        [Tooltip("레이저 판정 두께 (m)")]
        [SerializeField] private float _laserThickness      = 0.4f;
        [Tooltip("레이저 데미지")]
        [SerializeField] private float _laserDamage         = 15f;
        [Tooltip("레이저 감지 레이어마스크 (플레이어 레이어)")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("이펙트")]
        [Tooltip("레이저 전조 이펙트 프리팹 (SkillEffectActor 등)")]
        [SerializeField] private GameObject _windupEffectPrefab;
        [Tooltip("레이저 본체 이펙트 프리팹")]
        [SerializeField] private GameObject _laserEffectPrefab;

        private GameObject _activeLaserEffect;

        // ── MinionBase 구현 ───────────────────────────────────────────────

        protected override IEnumerator AttackCoroutine()
        {
            // ① 전조 이펙트 표시
            if (_windupEffectPrefab != null)
                Instantiate(_windupEffectPrefab, transform.position, GetFacingRotation());

            yield return new WaitForSeconds(_laserWindupDuration);

            if (_stats.IsDead) yield break;

            // ② 레이저 이펙트 생성
            if (_laserEffectPrefab != null)
            {
                _activeLaserEffect = Instantiate(
                    _laserEffectPrefab,
                    transform.position,
                    GetFacingRotation());
            }

            // ③ 데미지 판정 (BoxCast2D로 레이저 범위 내 플레이어 감지)
            ApplyLaserDamage();

            yield return new WaitForSeconds(_laserDuration);

            // ④ 레이저 이펙트 제거
            if (_activeLaserEffect != null)
                Destroy(_activeLaserEffect);
        }

        protected override void OnForceCleanup()
        {
            if (_activeLaserEffect != null)
                Destroy(_activeLaserEffect);
        }

        // ── 레이저 데미지 ─────────────────────────────────────────────────

        private void ApplyLaserDamage()
        {
            // 레이저 시작점: 하수인 위치에서 정면 방향으로 halfLength 만큼 오프셋된 중심
            Vector2 origin    = (Vector2)transform.position + Vector2.right * _facingDir * (_laserLength * 0.5f);
            Vector2 direction = Vector2.right * _facingDir;

            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                origin,
                new Vector2(_laserLength, _laserThickness),
                0f,
                direction,
                0f,
                _playerLayer);

            foreach (var hit in hits)
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;
                if (damageable.IsInvincible) continue;

                damageable.TakeDamage(new HitInfo
                {
                    Damage         = _laserDamage,
                    DamageType     = DamageType.Magic,
                    SourcePosition = transform.position,
                    KnockbackForce = 0f,
                });
            }
        }

        private Quaternion GetFacingRotation()
        {
            // 레이저 이펙트를 정면 방향으로 회전
            return _facingDir >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        }

        // ── Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 1f, 0.4f);
            Vector3 dir    = Vector3.right * _facingDir;
            Vector3 center = transform.position + dir * (_laserLength * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(_laserLength, _laserThickness, 0f));
        }
#endif
    }
}
