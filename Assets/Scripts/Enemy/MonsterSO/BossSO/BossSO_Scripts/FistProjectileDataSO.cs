using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "FistProjectileData", menuName = "2D Roguelike/Boss/Fist Projectile Data")]
    public class FistProjectileDataSO : ScriptableObject
    {
        [Header("주먹 발사체")]
        [SerializeField] private float _moveSpeed     = 15f;
        [SerializeField] private float _damage        = 25f;
        [SerializeField] private float _maxTravelTime = 3f;

        public float MoveSpeed     => _moveSpeed;
        public float Damage        => _damage;
        public float MaxTravelTime => _maxTravelTime;
    }
}
