using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossPartHPData", menuName = "2D Roguelike/Boss/Boss Part HP Data")]
    public class BossPartHPDataSO : ScriptableObject
    {
        [Header("파츠 HP")]
        [SerializeField] private float _maxHp            = 1000f;
        [SerializeField] private float _damageMultiplier = 1f;

        [Header("피격 사운드")]
        [SerializeField] private AudioClip[] _hitClips;

        public float       MaxHp            => _maxHp;
        public float       DamageMultiplier => _damageMultiplier;
        public AudioClip[] HitClips         => _hitClips;
    }
}
