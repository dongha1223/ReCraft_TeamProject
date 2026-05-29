using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "MidBossData", menuName = "2D Roguelike/Enemy/Mid Boss Data")]
    public class MidBossDataSO : EnemyDataSO
    {
        [Header("근접 공격")]
        [SerializeField] private float _attackDamage   = 15f;
        [SerializeField] private float _knockbackForce = 5f;

        [Header("내려찍기 스킬")]
        [SerializeField] private AreaSkillSpec _slamSpec;
        [SerializeField] private float         _slamCooldown  = 10f;
        [SerializeField] private float         _jumpForce     = 14f;
        [SerializeField] private float         _jumpHangTime  = 0.5f;
        [SerializeField] private float         _slamDownSpeed = 20f;
        [SerializeField] private float         _slamLandTime  = 0.25f;
        [SerializeField] private float         _boxStep       = 1.5f;
        [SerializeField] private float         _boxInterval   = 0.3f;
        [SerializeField] private float         _slamEndLag    = 0.3f;

        [Header("슬램 이펙트")]
        [SerializeField] private GameObject _slamEffectPrefab;

        public float         AttackDamage     => _attackDamage;
        public float         KnockbackForce   => _knockbackForce;
        public AreaSkillSpec SlamSpec         => _slamSpec;
        public float         SlamCooldown     => _slamCooldown;
        public float         JumpForce        => _jumpForce;
        public float         JumpHangTime     => _jumpHangTime;
        public float         SlamDownSpeed    => _slamDownSpeed;
        public float         SlamLandTime     => _slamLandTime;
        public float         BoxStep          => _boxStep;
        public float         BoxInterval      => _boxInterval;
        public float         SlamEndLag       => _slamEndLag;
        public GameObject    SlamEffectPrefab => _slamEffectPrefab;
    }
}
