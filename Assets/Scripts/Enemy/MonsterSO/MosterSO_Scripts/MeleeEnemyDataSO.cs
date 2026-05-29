using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "MeleeEnemyData", menuName = "2D Roguelike/Enemy/Melee Enemy Data")]
    public class MeleeEnemyDataSO : EnemyDataSO
    {
        [Header("근접 공격")]
        [SerializeField] private float _attackDamage   = 10f;
        [SerializeField] private float _knockbackForce = 4f;

        public float AttackDamage   => _attackDamage;
        public float KnockbackForce => _knockbackForce;
    }
}
