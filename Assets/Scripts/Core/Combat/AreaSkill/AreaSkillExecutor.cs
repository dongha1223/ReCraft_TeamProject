using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 즉발형 범위 스킬의 판정·데미지 배포 실행기.
    ///
    /// 사용법:
    ///   1. 적·플레이어 오브젝트에 컴포넌트로 추가한다.
    ///   2. 플레이어가 시전자라면 _statController를 연결한다 (타입 배율 적용).
    ///      적이 시전자라면 null로 두면 BaseDamage가 그대로 사용된다.
    ///   3. AttackCoroutine 등에서 Execute(spec, origin, forward)를 호출한다.
    ///
    /// 지원 형태:
    ///   Circle — OverlapCircleAll, 넉백 방향 = 폭발 중심 → 대상
    ///   Box    — OverlapBoxAll, 직사각형 즉발 판정
    ///   Cone   — OverlapCircleAll + 각도 필터, forward 방향 기준 부채꼴
    /// </summary>
    public class AreaSkillExecutor : MonoBehaviour
    {
        [Header("플레이어 전용 (적은 null)")]
        [Tooltip("플레이어 시전 시 연결. PhysicalPower/MagicPower/CriticalPower 배율을 가져온다.")]
        [SerializeField] private PlayerStatController _statController;

        // ── 공개 실행 진입점 ──────────────────────────────────────────

        /// <summary>
        /// 범위 스킬을 즉시 실행한다.
        /// </summary>
        /// <param name="spec">스킬 설정 에셋</param>
        /// <param name="origin">범위 중심 월드 좌표</param>
        /// <param name="forward">Cone 판정 기준 방향 (Circle·Box는 무시)</param>
        public void Execute(AreaSkillSpec spec, Vector2 origin, Vector2 forward)
        {
            if (spec == null) return;

            Collider2D[] raw = QueryShape(spec, origin, forward);

            List<IDamageable> targets = TargetCollector2D.CollectUnique(
                raw,
                spec.RequireLineOfSight,
                origin,
                spec.ObstacleLayer);

            float typeMultiplier = GetTypeMultiplier(spec.DamageType);

            foreach (var target in targets)
            {
                // Component 캐스팅으로 위치 접근 (IDamageable은 항상 MonoBehaviour)
                var comp = target as Component;
                if (comp == null) continue;

                Vector2 targetPos = comp.transform.position;
                float   damage    = CalculateDamage(spec, typeMultiplier, origin, targetPos);

                target.TakeDamage(new HitInfo
                {
                    Damage             = damage,
                    DamageType         = spec.DamageType,
                    SourcePosition     = origin,      // KnockbackReceiver가 방향 자동 계산
                    KnockbackForce     = spec.KnockbackForce,
                    IgnoreInvincibility = false,
                    StatusEffects      = spec.StatusEffects,
                });
            }
        }

        // ── 내부: 형태별 탐색 ─────────────────────────────────────────

        private Collider2D[] QueryShape(AreaSkillSpec spec, Vector2 origin, Vector2 forward)
        {
            switch (spec.ShapeType)
            {
                case AreaShapeType.Circle:
                    return Physics2D.OverlapCircleAll(origin, spec.Radius, spec.TargetLayer);

                case AreaShapeType.Box:
                    return Physics2D.OverlapBoxAll(origin, spec.BoxSize, spec.BoxRotation, spec.TargetLayer);

                case AreaShapeType.Cone:
                    return QueryCone(spec, origin, forward);

                default:
                    return System.Array.Empty<Collider2D>();
            }
        }

        private Collider2D[] QueryCone(AreaSkillSpec spec, Vector2 origin, Vector2 forward)
        {
            if (forward == Vector2.zero) forward = Vector2.right;

            Collider2D[] candidates = Physics2D.OverlapCircleAll(origin, spec.Radius, spec.TargetLayer);
            var result = new List<Collider2D>(candidates.Length);
            float halfAngle = spec.ConeAngle * 0.5f;

            foreach (var c in candidates)
            {
                Vector2 toTarget = (Vector2)c.transform.position - origin;
                if (toTarget == Vector2.zero) continue;
                if (Vector2.Angle(forward, toTarget) <= halfAngle)
                    result.Add(c);
            }

            return result.ToArray();
        }

        // ── 내부: 데미지 계산 ─────────────────────────────────────────

        private float CalculateDamage(AreaSkillSpec spec, float typeMultiplier, Vector2 origin, Vector2 targetPos)
        {
            float dmg = spec.BaseDamage * typeMultiplier;

            if (!spec.UseDistanceFalloff) return dmg;

            // 형태에 따라 최대 거리 기준 결정
            float maxDist = spec.ShapeType == AreaShapeType.Box
                ? Mathf.Max(spec.BoxSize.x, spec.BoxSize.y) * 0.5f
                : spec.Radius;

            float t       = Mathf.Clamp01(Vector2.Distance(origin, targetPos) / Mathf.Max(0.001f, maxDist));
            float falloff = Mathf.Lerp(spec.FalloffInnerMultiplier, spec.FalloffOuterMultiplier, t);
            return dmg * falloff;
        }

        /// <summary>
        /// DamageType에 대응하는 StatType 배율을 StatService에서 가져온다.
        /// _statController가 없으면 1f 반환 (적 시전자용).
        /// 해당 스탯이 미설정(baseValue = 0)이면 1f 반환 (안전 처리).
        /// </summary>
        private float GetTypeMultiplier(DamageType type)
        {
            if (_statController == null) return 1f;

            var svc = _statController.StatService;

            StatType stat = type switch
            {
                DamageType.Magic    => StatType.MagicPower,
                DamageType.Critical => StatType.CriticalPower,
                _                   => StatType.PhysicalPower,
            };

            float baseVal = svc.GetBaseValue(stat);
            return baseVal <= 0f ? 1f : svc.GetFinalValue(stat);
        }

        // ── 에디터 시각화 ─────────────────────────────────────────────

#if UNITY_EDITOR
        [Header("에디터 Gizmo (디버그용)")]
        [SerializeField] private AreaSkillSpec _previewSpec;

        private void OnDrawGizmosSelected()
        {
            if (_previewSpec == null) return;

            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.4f);
            Vector2 origin  = transform.position;
            Vector2 forward = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

            switch (_previewSpec.ShapeType)
            {
                case AreaShapeType.Circle:
                    Gizmos.DrawWireSphere(origin, _previewSpec.Radius);
                    break;

                case AreaShapeType.Box:
                    Gizmos.matrix = Matrix4x4.TRS(
                        origin,
                        Quaternion.Euler(0f, 0f, _previewSpec.BoxRotation),
                        Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, _previewSpec.BoxSize);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;

                case AreaShapeType.Cone:
                    DrawConeGizmo(origin, forward, _previewSpec.Radius, _previewSpec.ConeAngle);
                    break;
            }
        }

        private static void DrawConeGizmo(Vector2 origin, Vector2 forward, float radius, float angle)
        {
            float half      = angle * 0.5f;
            float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

            // 호 근사: 16등분
            int   segments  = 16;
            float step      = angle / segments;
            float startDeg  = baseAngle - half;

            Vector3 prev = origin + new Vector2(
                Mathf.Cos(startDeg * Mathf.Deg2Rad),
                Mathf.Sin(startDeg * Mathf.Deg2Rad)) * radius;

            Gizmos.DrawLine(origin, prev);

            for (int i = 1; i <= segments; i++)
            {
                float deg  = (startDeg + step * i) * Mathf.Deg2Rad;
                Vector3 cur = origin + new Vector2(Mathf.Cos(deg), Mathf.Sin(deg)) * radius;
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }

            Gizmos.DrawLine(prev, origin);
        }
#endif
    }
}
