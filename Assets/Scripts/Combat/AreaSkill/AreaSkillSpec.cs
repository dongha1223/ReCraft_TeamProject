using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 범위형 스킬의 모든 설정 데이터.
    /// ScriptableObject로 만들어 에셋으로 관리한다.
    ///
    /// 사용 방법:
    ///   즉발형 — AreaSkillExecutor.Execute(spec, origin, forward) 호출
    ///   장판형 — AreaZoneActor.Initialize(owner, spec) 호출 (ZoneDuration > 0)
    ///
    /// 같은 범위 형태라도 이 에셋만 교체하면 데미지·상태이상·감쇠를 완전히 다르게 설정할 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAreaSkill", menuName = "2D Roguelike/Area Skill Spec")]
    public class AreaSkillSpec : ScriptableObject
    {
        // ── 범위 형태 ──────────────────────────────────────────────────
        [Header("범위 형태")]
        [Tooltip("탐색에 사용할 범위 형태")]
        public AreaShapeType ShapeType = AreaShapeType.Circle;

        [Tooltip("Circle·Cone 반지름 (m)")]
        public float Radius = 2f;

        [Tooltip("Box 크기 (가로, 세로)")]
        public Vector2 BoxSize = new Vector2(2f, 1f);

        [Tooltip("Box 회전각 (도). 0 = 시전자 로컬 기준 가로 방향")]
        public float BoxRotation = 0f;

        [Tooltip("Cone 전체 각도 (도). 90 = 좌우 각 45도")]
        [Range(1f, 360f)]
        public float ConeAngle = 90f;

        // ── 데미지 ────────────────────────────────────────────────────
        [Header("데미지")]
        [Tooltip("기본 데미지. 플레이어 시전 시 StatService 타입 배율이 추가 적용된다.")]
        public float BaseDamage = 20f;

        [Tooltip("데미지 유형 (물리/마법/치명)")]
        public DamageType DamageType = DamageType.Physical;

        // ── 넉백 ──────────────────────────────────────────────────────
        [Header("넉백")]
        [Tooltip("넉백 강도. 0이면 넉백 없음. 장판형은 보통 0으로 설정")]
        public float KnockbackForce = 0f;

        // ── 거리 감쇠 (선택) ──────────────────────────────────────────
        [Header("거리 감쇠 (선택)")]
        [Tooltip("true면 중심에서 멀수록 데미지가 줄어든다")]
        public bool UseDistanceFalloff = false;

        [Tooltip("범위 중심부 데미지 배율 (가까울수록)")]
        [Range(0.5f, 3f)]
        public float FalloffInnerMultiplier = 1.5f;

        [Tooltip("범위 가장자리 데미지 배율 (멀수록)")]
        [Range(0f, 1f)]
        public float FalloffOuterMultiplier = 0.5f;

        // ── 시야 차단 (선택) ──────────────────────────────────────────
        [Header("시야 차단 (선택)")]
        [Tooltip("true면 장애물 뒤 대상은 맞지 않는다")]
        public bool RequireLineOfSight = false;

        [Tooltip("시야 차단 레이어 (RequireLineOfSight = true일 때만 사용)")]
        public LayerMask ObstacleLayer;

        // ── 상태이상 ──────────────────────────────────────────────────
        [Header("상태이상")]
        [Tooltip("이 스킬이 적중 시 부여할 상태이상 목록. null이면 없음")]
        public StatusEffectSpec[] StatusEffects;

        // ── 대상 레이어 ───────────────────────────────────────────────
        [Header("대상 레이어")]
        [Tooltip("범위 탐색에 사용할 레이어마스크")]
        public LayerMask TargetLayer;

        // ── 장판형 전용 설정 (AreaZoneActor에서 사용) ─────────────────
        [Header("장판형 전용 (AreaZoneActor)")]
        [Tooltip("장판 지속 시간 (초). 즉발형은 0으로 놔두면 됨")]
        public float ZoneDuration = 5f;

        [Tooltip("틱 주기 (초). 이 간격마다 범위 안 대상에게 데미지를 준다")]
        public float ZoneTickInterval = 0.5f;

        [Tooltip("대상별 재피격 쿨다운 (초). 0이면 틱마다 무조건 적용")]
        public float ZonePerTargetCooldown = 0f;
    }
}
