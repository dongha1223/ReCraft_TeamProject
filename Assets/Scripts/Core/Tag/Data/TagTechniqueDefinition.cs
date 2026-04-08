using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 교체기 한 단계의 정의 데이터.
    /// FormDefinition.TagTechniques[0~2] 에 연결.
    /// Project 창 우클릭 → Create → Game/Tag Technique Definition
    ///
    /// Behaviour가 null이면 DefaultPhases(AreaSkillSpec[])를 순차 실행.
    /// Behaviour가 있으면 커스텀 로직(이동 포함 등)을 실행.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Tag Technique Definition", fileName = "NewTagTechnique")]
    public class TagTechniqueDefinition : ScriptableObject
    {
        [Header("단계 정보")]
        [SerializeField] private int   _level    = 1;
        [Tooltip("교체기 총 연출 시간(초). Execute 완료 후 이 시간까지 남은 만큼 후딜로 처리.")]
        [SerializeField] private float _duration = 0.5f;

        [Header("무적")]
        [Tooltip("교체기 시작 시점부터 적용되는 무적 시간(초).")]
        [SerializeField] private float _invincibleDuration = 0.3f;

        [Header("기본 실행 (Behaviour가 null일 때 사용)")]
        [Tooltip("순차 실행할 범위 스킬 목록. 각 스킬마다 PhaseDelays 딜레이 적용.")]
        [SerializeField] private AreaSkillSpec[] _defaultPhases;
        [Tooltip("각 Phase 실행 후 대기 시간(초). 길이가 부족하면 0으로 처리.")]
        [SerializeField] private float[]         _phaseDelays;

        [Header("커스텀 실행 로직")]
        [Tooltip("null이면 DefaultPhases 실행. 이동/특수 연출이 필요한 경우 여기에 연결.")]
        [SerializeField] private TagTechniqueBehaviour _behaviour;

        [Header("진입 폼 버프 (2, 3단계 권장)")]
        [SerializeField] private bool              _hasBuff        = false;
        [SerializeField] private StatType          _buffStat       = StatType.AttackPower;
        [SerializeField] private ModifierOperation _buffOperation  = ModifierOperation.Multiply;
        [Tooltip("Multiply: 1.3 = 30% 증가 / Add: 직접 수치 추가")]
        [SerializeField] private float             _buffValue      = 1.3f;
        [SerializeField] private float             _buffDuration   = 5f;

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public int   Level              => _level;
        public float Duration           => _duration;
        public float InvincibleDuration => _invincibleDuration;

        public AreaSkillSpec[]       DefaultPhases => _defaultPhases;
        public float[]               PhaseDelays   => _phaseDelays;
        public TagTechniqueBehaviour Behaviour     => _behaviour;

        public bool              HasBuff        => _hasBuff;
        public StatType          BuffStat       => _buffStat;
        public ModifierOperation BuffOperation  => _buffOperation;
        public float             BuffValue      => _buffValue;
        public float             BuffDuration   => _buffDuration;
    }
}
