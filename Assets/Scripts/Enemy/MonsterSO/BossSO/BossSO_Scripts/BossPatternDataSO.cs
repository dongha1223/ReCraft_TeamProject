using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossPatternData", menuName = "2D Roguelike/Boss/Boss Pattern Data")]
    public class BossPatternDataSO : ScriptableObject
    {
        [Header("Phase 1 — 패턴 간격")]
        [SerializeField] private float _phase1CooldownMin = 2f;
        [SerializeField] private float _phase1CooldownMax = 4f;

        [Header("패턴 1 — 조준 투사체")]
        [SerializeField] private GameObject _aimIndicatorPrefab;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float      _aimInterval         = 0.6f;
        [SerializeField] private float      _aimDuration         = 1.5f;
        [SerializeField] private float      _projectileDamage    = 18f;
        [SerializeField] private float      _projectileKnockback = 4f;
        [SerializeField] private float      _fireInterval        = 0.3f;

        [Header("패턴 2 — 하수인 공통")]
        [SerializeField] private GameObject _blackMinionPrefab;
        [SerializeField] private GameObject _whiteMinionPrefab;

        [Header("패턴 2-1 — 검은 하수인 수직")]
        [SerializeField] private float _pattern21SpawnOffsetX = 3f;
        [SerializeField] private float _pattern21BottomY      = -2f;
        [SerializeField] private float _pattern21VerticalStep = 2f;

        [Header("패턴 2-2 — 하얀 하수인 대각")]
        [SerializeField] private Vector2[] _pattern22Offsets = new Vector2[]
        {
            new Vector2( 3f,  1f), new Vector2( 3f, -1f),
            new Vector2(-3f, -1f), new Vector2(-3f,  1f),
        };

        [Header("패턴 2-3 — 하얀 하수인 수직")]
        [SerializeField] private Vector2[] _pattern23Offsets = new Vector2[]
        {
            new Vector2(4f, 3f), new Vector2(4f, 2f),
            new Vector2(4f, 1f), new Vector2(4f, 0f),
        };

        [Header("Phase 2 — 패턴 간격")]
        [SerializeField] private float _phase2CooldownMin = 2.5f;
        [SerializeField] private float _phase2CooldownMax = 4f;

        [Header("Phase 2 — 장판 스펙")]
        [SerializeField] private AreaSkillSpec _phase2ZoneSpec;

        [Header("Phase 2 패턴 A")]
        [SerializeField] private bool _patternA_IncludePlayerPlatform = false;

        [Header("Phase 2 패턴 C")]
        [SerializeField] private int   _patternC_RepeatCount = 4;
        [SerializeField] private float _patternC_RepeatDelay = 3f;

        public float         Phase1CooldownMin              => _phase1CooldownMin;
        public float         Phase1CooldownMax              => _phase1CooldownMax;
        public GameObject    AimIndicatorPrefab             => _aimIndicatorPrefab;
        public GameObject    ProjectilePrefab               => _projectilePrefab;
        public float         AimInterval                    => _aimInterval;
        public float         AimDuration                    => _aimDuration;
        public float         ProjectileDamage               => _projectileDamage;
        public float         ProjectileKnockback            => _projectileKnockback;
        public float         FireInterval                   => _fireInterval;
        public GameObject    BlackMinionPrefab              => _blackMinionPrefab;
        public GameObject    WhiteMinionPrefab              => _whiteMinionPrefab;
        public float         Pattern21SpawnOffsetX          => _pattern21SpawnOffsetX;
        public float         Pattern21BottomY               => _pattern21BottomY;
        public float         Pattern21VerticalStep          => _pattern21VerticalStep;
        public Vector2[]     Pattern22Offsets               => _pattern22Offsets;
        public Vector2[]     Pattern23Offsets               => _pattern23Offsets;
        public float         Phase2CooldownMin              => _phase2CooldownMin;
        public float         Phase2CooldownMax              => _phase2CooldownMax;
        public AreaSkillSpec Phase2ZoneSpec                 => _phase2ZoneSpec;
        public bool          PatternA_IncludePlayerPlatform => _patternA_IncludePlayerPlatform;
        public int           PatternC_RepeatCount           => _patternC_RepeatCount;
        public float         PatternC_RepeatDelay           => _patternC_RepeatDelay;
    }
}
