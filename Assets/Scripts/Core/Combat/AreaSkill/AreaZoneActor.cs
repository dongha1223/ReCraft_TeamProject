using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 지속형 장판 오브젝트.
    /// 일정 시간 동안 범위 안 대상에게 틱 주기마다 데미지와 상태이상을 준다.
    ///
    /// 사용법:
    ///   1. 빈 GameObject에 이 컴포넌트를 추가하고 프리팹으로 저장한다.
    ///   2. Instantiate 후 Initialize(owner, spec)를 즉시 호출한다.
    ///   3. 수명이 끝나면 자동으로 Destroy된다.
    ///
    /// 설계 원칙:
    ///   - 장판은 보통 넉백이 없거나 매우 약하게 설정한다 (KnockbackForce = 0 권장)
    ///   - 틱 간격(ZoneTickInterval)이 기본 재적용 주기이다
    ///   - ZonePerTargetCooldown > 0이면 틱과 무관하게 대상별 쿨다운을 따로 관리한다
    ///     (여러 장판이 겹쳐도 단일 대상의 최대 피격 빈도를 제어할 수 있다)
    /// </summary>
    public class AreaZoneActor : MonoBehaviour
    {
        private Transform     _owner;
        private AreaSkillSpec _spec;
        private float         _lifeTimer;
        private float         _tickTimer;

        // 대상별 재피격 허용 시각 (ZonePerTargetCooldown > 0일 때만 사용)
        private readonly Dictionary<IDamageable, float> _nextHitTime = new();

        // ── 초기화 ────────────────────────────────────────────────────

        /// <summary>
        /// Instantiate 직후 반드시 호출. 이전 상태를 초기화하고 스펙을 설정한다.
        /// </summary>
        /// <param name="owner">시전자 Transform (자기 자신 제외용, null 허용)</param>
        /// <param name="spec">장판 설정 에셋</param>
        public void Initialize(Transform owner, AreaSkillSpec spec)
        {
            _owner     = owner;
            _spec      = spec;
            _lifeTimer = 0f;
            _tickTimer = 0f;
            _nextHitTime.Clear();
        }

        // ── 수명 & 틱 관리 ────────────────────────────────────────────

        private void Update()
        {
            if (_spec == null) return;

            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= _spec.ZoneDuration)
            {
                Destroy(gameObject);
                return;
            }

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _spec.ZoneTickInterval)
            {
                _tickTimer -= _spec.ZoneTickInterval;
                Tick();
            }
        }

        // ── 틱 판정 ───────────────────────────────────────────────────

        private void Tick()
        {
            // 장판은 항상 원형 탐색 (spec.Radius 사용)
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                _spec.Radius,
                _spec.TargetLayer);

            List<IDamageable> targets = TargetCollector2D.CollectUnique(
                hits,
                _spec.RequireLineOfSight,
                transform.position,
                _spec.ObstacleLayer);

            float now = Time.time;

            foreach (var target in targets)
            {
                // 대상별 쿨다운 체크 (ZonePerTargetCooldown > 0일 때만)
                if (_spec.ZonePerTargetCooldown > 0f)
                {
                    if (_nextHitTime.TryGetValue(target, out float nextTime) && now < nextTime)
                        continue;

                    _nextHitTime[target] = now + _spec.ZonePerTargetCooldown;
                }

                target.TakeDamage(new HitInfo
                {
                    Damage              = _spec.BaseDamage,
                    DamageType          = _spec.DamageType,
                    SourcePosition      = transform.position,
                    KnockbackForce      = _spec.KnockbackForce,
                    IgnoreInvincibility = false,
                    StatusEffects       = _spec.StatusEffects,
                });
            }
        }

        // ── 에디터 시각화 ─────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_spec == null) return;

            // 활성 시간 비율에 따라 색상을 흰색→주황으로 전환
            float ratio = _spec.ZoneDuration > 0f ? _lifeTimer / _spec.ZoneDuration : 0f;
            Gizmos.color = Color.Lerp(
                new Color(1f, 0.8f, 0.2f, 0.5f),
                new Color(1f, 0.2f, 0.0f, 0.2f),
                ratio);

            Gizmos.DrawWireSphere(transform.position, _spec.Radius);
        }

        // 씬뷰에서 프리팹 상태일 때도 반지름을 볼 수 있도록
        private void OnDrawGizmos()
        {
            if (_spec != null) return;   // Initialize 후에는 Selected 버전이 담당
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, 2f);  // 기본 미리보기 반지름
        }
#endif
    }
}
