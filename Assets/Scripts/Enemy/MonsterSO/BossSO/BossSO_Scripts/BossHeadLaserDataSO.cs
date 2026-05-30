using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossHeadLaserData", menuName = "2D Roguelike/Boss/Boss Head Laser Data")]
    public class BossHeadLaserDataSO : ScriptableObject
    {
        [Header("피해")]
        [Tooltip("0.1초당 피해량")]
        [SerializeField] private float _damagePerTick = 1f;
        [Tooltip("피해 틱 간격 (초)")]
        [SerializeField] private float _tickInterval  = 0.1f;

        [Header("충돌 범위")]
        [Tooltip("머리 중심으로부터 레이저 유효 길이 (절반)")]
        [SerializeField] private float _laserHitLength = 12f;
        [Tooltip("레이저 충돌 폭 (양쪽 합산)")]
        [SerializeField] private float _laserHitWidth  = 1.2f;

        [Header("시각 스케일 (SpriteRenderer X)")]
        [Tooltip("레이저 스프라이트의 X 스케일 — 스프라이트 자연 폭에 맞게 조정")]
        [SerializeField] private float _laserScaleX = 3f;

        public float DamagePerTick => _damagePerTick;
        public float TickInterval  => _tickInterval;
        public float LaserHitLength => _laserHitLength;
        public float LaserHitWidth  => _laserHitWidth;
        public float LaserScaleX    => _laserScaleX;
    }
}
