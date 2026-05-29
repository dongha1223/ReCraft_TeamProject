using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossStatsData", menuName = "2D Roguelike/Boss/Boss Stats Data")]
    public class BossStatsSO : ScriptableObject
    {
        [Header("스탯")]
        [SerializeField] private float _maxHp = 1000f;
        [SerializeField] [Range(0.01f, 0.99f)] private float _phase2HpRatio = 0.5f;

        [Header("피격 사운드")]
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _heavyHitClips;

        public float       MaxHp         => _maxHp;
        public float       Phase2HpRatio => _phase2HpRatio;
        public AudioClip[] HitClips      => _hitClips;
        public AudioClip[] HeavyHitClips => _heavyHitClips;
    }
}
