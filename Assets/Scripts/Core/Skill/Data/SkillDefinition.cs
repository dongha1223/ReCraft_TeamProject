using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 폼 스킬 하나의 정의 데이터.
    /// FormDefinition.Skills[0~1] 에 연결.
    /// Project 창 우클릭 → Create → Game/Skill Definition
    ///
    /// Type = Area   : AreaPhases를 순차 실행 (AreaSkillExecutor 재사용)
    /// Type = Custom : Behaviour에 위임 (이동·특수 연출 등 자유로운 로직)
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Definition", fileName = "NewSkillDefinition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("공통")]
        [SerializeField] private SkillType  _type       = SkillType.Custom;
        [SerializeField] private float      _cooldown   = 1.5f;
        [Tooltip("데미지 유형 (물리/마법/치명). HitInfo.DamageType과 StatService 배율에 영향.")]
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [Tooltip("데미지 계산 기준 StatType. FormSkillController가 폼 전환 시 BaseDamage를 이 타입으로 등록한다.")]
        [SerializeField] private StatType   _statType   = StatType.SkillPower;
        [Tooltip("이 스킬의 기본 데미지. StatService에 기본값으로 등록된다.")]
        [SerializeField] private float      _baseDamage = 20f;

        [Header("Area 타입 설정 (Type = Area일 때 사용)")]
        [SerializeField] private AreaSkillSpec[] _areaPhases;
        [Tooltip("각 Phase 실행 후 대기 시간(초). 길이가 부족하면 0으로 처리.")]
        [SerializeField] private float[]         _phaseDelays;

        [Header("Custom 타입 설정 (Type = Custom일 때 사용)")]
        [Tooltip("null이면 아무 동작도 없음. 이동·투사체 등 로직은 여기에 연결.")]
        [SerializeField] private SkillBehaviour _behaviour;

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public SkillType      Type           => _type;
        public float          Cooldown       => _cooldown;
        public DamageType     DamageType     => _damageType;
        public StatType       DamageStatType => _statType;
        public float          BaseDamage     => _baseDamage;
        public AreaSkillSpec[] AreaPhases    => _areaPhases;
        public float[]         PhaseDelays   => _phaseDelays;
        public SkillBehaviour  Behaviour     => _behaviour;
    }
}
