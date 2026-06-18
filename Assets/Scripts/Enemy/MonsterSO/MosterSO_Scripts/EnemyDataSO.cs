using UnityEngine;

namespace _2D_Roguelike
{
    public abstract class EnemyDataSO : ScriptableObject
    {
        [Header("스탯")]
        [SerializeField] private float _maxHp = 70f;

        [Header("이동")]
        [SerializeField] private float _moveSpeed      = 2f;
        [SerializeField] private float _patrolDistance = 3f;

        [Header("감지 & 공격")]
        [SerializeField] private float _detectionRange = 5f;
        [SerializeField] private float _attackRange    = 0.8f;
        [SerializeField] private float _attackCooldown = 1.2f;
        [SerializeField] private float _preAttackDelay = 0f;

        [Header("플랫폼 인식")]
        [SerializeField] private float _platformYThreshold = 1.5f;

        [Header("발판 이탈 방지")]
        [SerializeField] private float _ledgeCheckOffsetX = 0.4f;
        [SerializeField] private float _ledgeCheckOffsetY = -0.1f;
        [SerializeField] private float _ledgeCheckDist    = 0.8f;

        [Header("피격 사운드")]
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _heavyHitClips;

        public float       MaxHp              => _maxHp;
        public float       MoveSpeed          => _moveSpeed;
        public float       PatrolDistance     => _patrolDistance;
        public float       DetectionRange     => _detectionRange;
        public float       AttackRange        => _attackRange;
        public float       AttackCooldown     => _attackCooldown;
        public float       PreAttackDelay     => _preAttackDelay;
        public float       PlatformYThreshold => _platformYThreshold;
        public float       LedgeCheckOffsetX  => _ledgeCheckOffsetX;
        public float       LedgeCheckOffsetY  => _ledgeCheckOffsetY;
        public float       LedgeCheckDist     => _ledgeCheckDist;
        public AudioClip[] HitClips           => _hitClips;
        public AudioClip[] HeavyHitClips      => _heavyHitClips;
    }
}
