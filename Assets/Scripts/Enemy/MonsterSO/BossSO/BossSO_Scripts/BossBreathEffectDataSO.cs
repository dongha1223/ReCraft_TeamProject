using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossBreathEffectData", menuName = "2D Roguelike/Boss/Boss Breath Effect Data")]
    public class BossBreathEffectDataSO : ScriptableObject
    {
        [Header("호흡 효과")]
        [SerializeField] private float _breathSpeed  = 1.2f;
        [SerializeField] private float _breathAmount = 0.03f;

        public float BreathSpeed  => _breathSpeed;
        public float BreathAmount => _breathAmount;
    }
}
