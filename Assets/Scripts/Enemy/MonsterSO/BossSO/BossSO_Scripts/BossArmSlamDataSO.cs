using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossArmSlamData", menuName = "2D Roguelike/Boss/Boss Arm Slam Data")]
    public class BossArmSlamDataSO : ScriptableObject
    {
        [Header("들어올리기")]
        [SerializeField] private float _liftHeight   = 2f;
        [SerializeField] private float _liftDuration  = 1.2f;

        [Header("떨림")]
        [SerializeField] private float _trembleDuration  = 1.2f;
        [SerializeField] private float _trembleFrequency = 8f;
        [SerializeField] private float _trembleAmplitude = 0.15f;

        [Header("슬램")]
        [SerializeField] private float _slamDuration = 0.15f;

        [Header("피해")]
        [SerializeField] private float _slamDamage    = 30f;
        [SerializeField] private float _slamKnockback = 8f;

        [Header("피해 범위")]
        [SerializeField] private float _slamRangeX = 6f;
        [SerializeField] private float _slamRangeY = 2f;

        [Header("슬램 화면 흔들림")]
        [SerializeField] private float _shakeDuration  = 0.3f;
        [SerializeField] private float _shakeMagnitude = 0.25f;

        public float LiftHeight       => _liftHeight;
        public float LiftDuration     => _liftDuration;
        public float TrembleDuration  => _trembleDuration;
        public float TrembleFrequency => _trembleFrequency;
        public float TrembleAmplitude => _trembleAmplitude;
        public float SlamDuration     => _slamDuration;
        public float SlamDamage       => _slamDamage;
        public float SlamKnockback    => _slamKnockback;
        public float SlamRangeX       => _slamRangeX;
        public float SlamRangeY       => _slamRangeY;
        public float ShakeDuration    => _shakeDuration;
        public float ShakeMagnitude   => _shakeMagnitude;
    }
}
