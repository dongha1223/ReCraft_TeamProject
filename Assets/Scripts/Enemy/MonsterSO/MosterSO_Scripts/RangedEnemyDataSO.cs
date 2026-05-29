using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "RangedEnemyData", menuName = "2D Roguelike/Enemy/Ranged Enemy Data")]
    public class RangedEnemyDataSO : EnemyDataSO
    {
        [Header("원거리 공격")]
        [SerializeField] private float      _attackDamage    = 8f;
        [SerializeField] private float      _knockbackForce  = 3f;
        [SerializeField] private float      _windupDuration  = 1f;
        [SerializeField] private GameObject _projectilePrefab;

        public float      AttackDamage     => _attackDamage;
        public float      KnockbackForce   => _knockbackForce;
        public float      WindupDuration   => _windupDuration;
        public GameObject ProjectilePrefab => _projectilePrefab;
    }
}
