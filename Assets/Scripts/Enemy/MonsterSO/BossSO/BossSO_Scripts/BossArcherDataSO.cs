using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossArcherData", menuName = "2D Roguelike/Boss/Boss Archer Data")]
    public class BossArcherDataSO : ScriptableObject
    {
        [Header("이동 & 순찰")]
        [SerializeField] private float _moveSpeed       = 2f;
        [SerializeField] private float _chaseStartRange = 7f;
        [SerializeField] private float _chaseStopRange  = 5f;
        [SerializeField] private float _patrolDistance  = 4f;
        [SerializeField] private float _patrolChangeMin = 1f;
        [SerializeField] private float _patrolChangeMax = 3f;

        [Header("패턴 쿨타임")]
        [SerializeField] private float _cooldownMin = 2f;
        [SerializeField] private float _cooldownMax = 4f;

        [Header("패턴 목록")]
        [SerializeField] private BossPattern[] _patterns;

        [Header("Attack2 — 박스 일직선 발사")]
        [SerializeField] private AreaSkillSpec _attack2Spec;
        [SerializeField] private int           _attack2BoxCount      = 4;
        [SerializeField] private float         _attack2BoxStep       = 1.5f;
        [SerializeField] private float         _attack2BoxInterval   = 0.3f;
        [SerializeField] private float         _attack2PreDelay      = 0.5f;
        [SerializeField] private GameObject    _attack2EffectPrefab;
        [SerializeField] private float         _attack2EffectOffsetY = 0f;
        [SerializeField] private float         _attack2DamageDelay   = 0.5f;

        public float         MoveSpeed            => _moveSpeed;
        public float         ChaseStartRange      => _chaseStartRange;
        public float         ChaseStopRange       => _chaseStopRange;
        public float         PatrolDistance       => _patrolDistance;
        public float         PatrolChangeMin      => _patrolChangeMin;
        public float         PatrolChangeMax      => _patrolChangeMax;
        public float         CooldownMin          => _cooldownMin;
        public float         CooldownMax          => _cooldownMax;
        public BossPattern[] Patterns             => _patterns;
        public AreaSkillSpec Attack2Spec          => _attack2Spec;
        public int           Attack2BoxCount      => _attack2BoxCount;
        public float         Attack2BoxStep       => _attack2BoxStep;
        public float         Attack2BoxInterval   => _attack2BoxInterval;
        public float         Attack2PreDelay      => _attack2PreDelay;
        public GameObject    Attack2EffectPrefab  => _attack2EffectPrefab;
        public float         Attack2EffectOffsetY => _attack2EffectOffsetY;
        public float         Attack2DamageDelay   => _attack2DamageDelay;
    }
}
