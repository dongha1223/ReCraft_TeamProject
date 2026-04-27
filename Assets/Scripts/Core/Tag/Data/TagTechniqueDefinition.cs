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
        [Tooltip("폼 체인지까지 대기하는 시간(초).\n" +
                 "Behaviour가 연결된 경우 Behaviour는 비블로킹으로 실행되므로\n" +
                 "이 값이 경과하는 즉시 폼 체인지가 발생한다.\n\n" +
                 "※ 스프라이트·이동·카메라 등 시간 있는 연출을 포함하는 Behaviour라면\n" +
                 "   Duration을 연출 총 시간 이상으로 설정할 것.")]
        [SerializeField] private float _duration = 0.5f;

        [Header("무적")]
        [Tooltip("교체기 시작 시점부터 적용되는 무적 시간(초).")]
        [SerializeField] private float _invincibleDuration = 0.3f;

        [Header("기본 실행 (Behaviour가 null일 때 사용)")]
        [Tooltip("순차 실행할 범위 스킬 목록. 각 스킬마다 PhaseDelays 딜레이 적용.")]
        [SerializeField] private AreaSkillSpec[] _defaultPhases;
        [Tooltip("각 Phase 실행 후 대기 시간(초). 길이가 부족하면 0으로 처리.")]
        [SerializeField] private float[]         _phaseDelays;

        [Header("컷인 연출 (선택)")]
        [Tooltip("교체기 발동 전 재생할 컷인 연출 데이터.\n" +
                 "null이면 건너뛴다.\n" +
                 "컷신이 완전히 끝난 뒤 Behaviour가 발동되며,\n" +
                 "Duration은 컷신 종료 시점부터 카운트된다.")]
        [SerializeField] private CutinSequenceData _preCutscene;

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
        public CutinSequenceData     PreCutscene   => _preCutscene;
        public TagTechniqueBehaviour Behaviour     => _behaviour;

        public bool              HasBuff        => _hasBuff;
        public StatType          BuffStat       => _buffStat;
        public ModifierOperation BuffOperation  => _buffOperation;
        public float             BuffValue      => _buffValue;
        public float             BuffDuration   => _buffDuration;
    }
}
