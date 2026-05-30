using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "BossHeadFinalPhaseData", menuName = "2D Roguelike/Boss/Boss Head Final Phase Data")]
    public class BossHeadFinalPhaseDataSO : ScriptableObject
    {
        [Header("낙하")]
        [Tooltip("낙하 후 머리의 localY (Boss_Main 기준)")]
        [SerializeField] private float _fallLocalY     = -3f;
        [Tooltip("낙하 시 Z축 기울기 각도 (양수=왼쪽, 음수=오른쪽)")]
        [SerializeField] private float _fallTiltAngle  = 20f;
        [Tooltip("낙하 애니메이션 시간 (초) — 느릴수록 중력감 증가")]
        [SerializeField] private float _fallDuration   = 1.5f;

        [Header("착지 카메라 흔들림")]
        [SerializeField] private float _slamShakeDuration  = 0.45f;
        [SerializeField] private float _slamShakeMagnitude = 0.4f;

        [Header("착지 후 대기")]
        [SerializeField] private float _pauseAfterFall = 1.0f;

        [Header("상승")]
        [Tooltip("떠있을 때 머리의 localY (Boss_Main 기준)")]
        [SerializeField] private float _floatLocalY  = 3f;
        [Tooltip("상승 애니메이션 시간 (초)")]
        [SerializeField] private float _riseDuration = 2.5f;

        [Header("둥둥 효과 (상승 완료 후 타격 가능)")]
        [Tooltip("위아래 왕복 폭 (유닛)")]
        [SerializeField] private float _bobAmplitude = 0.15f;
        [Tooltip("초당 왕복 횟수")]
        [SerializeField] private float _bobFrequency = 0.8f;

        public float FallLocalY         => _fallLocalY;
        public float FallTiltAngle      => _fallTiltAngle;
        public float FallDuration       => _fallDuration;
        public float SlamShakeDuration  => _slamShakeDuration;
        public float SlamShakeMagnitude => _slamShakeMagnitude;
        public float PauseAfterFall     => _pauseAfterFall;
        public float FloatLocalY        => _floatLocalY;
        public float RiseDuration       => _riseDuration;
        public float BobAmplitude       => _bobAmplitude;
        public float BobFrequency       => _bobFrequency;
    }
}
