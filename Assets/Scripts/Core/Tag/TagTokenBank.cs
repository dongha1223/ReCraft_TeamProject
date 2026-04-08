using System;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 교체 토큰 게이지 관리.
    ///
    /// 구조:
    ///   _totalGauge = 0 ~ 300 (연속 float)
    ///   게이지 1개 = 100 단위
    ///   FilledCount = floor(_totalGauge / 100) → 완충된 토큰 수 (0~3)
    ///
    /// 예시:
    ///   토큰1[100%] 토큰2[100%] 토큰3[50%] → _totalGauge = 250
    ///   FilledCount = 2 → 2단계 교체기 발동 가능
    ///   ConsumeAll() → level=2 반환, _totalGauge=0 으로 전량 초기화
    ///   (3번째 50% 게이지도 함께 소멸)
    /// </summary>
    public class TagTokenBank : MonoBehaviour
    {
        [Header("토큰 설정")]
        [Tooltip("적 처치 시 획득하는 게이지량. 100 = 토큰 1개 완충.")]
        [SerializeField] private float _gainPerKill = 50f;

        private const float GaugePerToken = 100f;
        private const float MaxGauge      = 300f;

        private float _totalGauge;

        // ── 프로퍼티 ──────────────────────────────────────────────────
        /// <summary>완전히 채워진 토큰 수 (0~3)</summary>
        public int FilledCount => Mathf.FloorToInt(_totalGauge / GaugePerToken);

        /// <summary>교체 가능 여부 (완충 토큰 1개 이상)</summary>
        public bool HasAny => FilledCount >= 1;

        /// <summary>현재 전체 게이지 (UI용, 0~300)</summary>
        public float TotalGauge => _totalGauge;

        /// <summary>게이지 변경 시 발생 (totalGauge 값 전달, UI 갱신용)</summary>
        public event Action<float> OnGaugeChanged;

        // ── 외부 접근용 설정값 ────────────────────────────────────────
        public float GainPerKill => _gainPerKill;

        // ── 조작 ──────────────────────────────────────────────────────
        /// <summary>게이지 획득. 적 처치, 대시 회피 등에서 호출.</summary>
        public void Gain(float amount)
        {
            _totalGauge = Mathf.Clamp(_totalGauge + amount, 0f, MaxGauge);
            OnGaugeChanged?.Invoke(_totalGauge);
        }

        /// <summary>
        /// 교체 시 전량 소비.
        /// 반환값 = 소비된 완충 토큰 수 (교체기 레벨 1~3).
        /// 완충 토큰이 없으면 0 반환 (호출 전 HasAny 확인 권장).
        /// </summary>
        public int ConsumeAll()
        {
            int level = FilledCount;
            _totalGauge = 0f;
            OnGaugeChanged?.Invoke(0f);
            return level;
        }

#if UNITY_EDITOR
        // 에디터에서 게이지 상태 확인용
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            GUI.Label(new Rect(10, 10, 300, 20),
                $"TokenGauge: {_totalGauge:F0}/300  Filled: {FilledCount}");
        }
#endif
    }
}
