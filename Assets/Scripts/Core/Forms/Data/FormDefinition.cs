using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 폼(캐릭터) 하나의 정의 데이터.
    /// Project 창 우클릭 → Create → Game/Form Definition 으로 생성.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Form Definition", fileName = "NewFormDefinition")]
    public class FormDefinition : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string _formId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [Header("비주얼")]
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Vector3 _baseScale = Vector3.one;

        [Header("콜라이더")]
        [SerializeField] private Vector2 _colliderSize   = new Vector2(0.5f, 1f);
        [SerializeField] private Vector2 _colliderOffset = Vector2.zero;

        [Header("데미지 타입")]
        [SerializeField] private DamageType _primaryDamageType = DamageType.Physical;

        [Header("기본 스탯")]
        [SerializeField] private float _baseAttackPower = 10f;

        [Header("스킬 (인덱스 0=A키, 1=S키)")]
        [Tooltip("이 폼의 A/S 스킬. FormSkillController가 참조.")]
        [SerializeField] private SkillDefinition[] _skills = new SkillDefinition[2];

        [Header("교체기 (인덱스 0=1단계, 1=2단계, 2=3단계)")]
        [Tooltip("이 폼이 진입할 때 사용하는 교체기. 토큰 소비량에 따라 단계 결정.")]
        [SerializeField] private TagTechniqueDefinition[] _tagTechniques = new TagTechniqueDefinition[3];

        // ── 프로퍼티 ──────────────────────────────────────────────────
        public string FormId              => _formId;
        public string DisplayName         => _displayName;
        public Sprite Icon                => _icon;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public Sprite IdleSprite          => _idleSprite;
        public Vector3 BaseScale          => _baseScale;
        public Vector2 ColliderSize       => _colliderSize;
        public Vector2 ColliderOffset     => _colliderOffset;
        public DamageType PrimaryDamageType => _primaryDamageType;
        public float BaseAttackPower      => _baseAttackPower;
        public SkillDefinition[]        Skills        => _skills;
        public TagTechniqueDefinition[] TagTechniques => _tagTechniques;
    }
}
