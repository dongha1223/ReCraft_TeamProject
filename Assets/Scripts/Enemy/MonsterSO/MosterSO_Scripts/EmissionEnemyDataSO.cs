using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "EmissionEnemyData", menuName = "2D Roguelike/Enemy/Emission Enemy Data")]
    public class EmissionEnemyDataSO : EnemyDataSO
    {
        [Header("방사형 공격")]
        [SerializeField] private float      _attackDamage    = 8f;
        [SerializeField] private float      _knockbackForce  = 2f;
        [SerializeField] private float      _windupDuration  = 0.8f;
        [SerializeField] private int        _projectileCount = 6;
        [SerializeField] private float      _fireInterval    = 0.05f;
        [SerializeField] private GameObject _projectilePrefab;

        public float      AttackDamage     => _attackDamage;
        public float      KnockbackForce   => _knockbackForce;
        public float      WindupDuration   => _windupDuration;
        public int        ProjectileCount  => _projectileCount;
        public float      FireInterval     => _fireInterval;
        public GameObject ProjectilePrefab => _projectilePrefab;
    }
}
