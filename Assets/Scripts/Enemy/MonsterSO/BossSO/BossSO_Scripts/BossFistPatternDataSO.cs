using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossFistPatternData", menuName = "2D Roguelike/Boss/Boss Fist Pattern Data")]
    public class BossFistPatternDataSO : ScriptableObject
    {
        [Header("타이밍 (초)")]
        [SerializeField] private float _aimDuration    = 3f;
        [SerializeField] private float _respawnDelay   = 5f;
        [SerializeField] private float _idleDelay      = 2f;
        [SerializeField] private float _rightArmOffset = 6f;

        public float AimDuration    => _aimDuration;
        public float RespawnDelay   => _respawnDelay;
        public float IdleDelay      => _idleDelay;
        public float RightArmOffset => _rightArmOffset;
    }
}
