using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 태그기 컷인 연출 데이터.
    /// 캐릭터별·단계별로 에셋을 분리해 CutinIllustPlayer에 전달한다.
    ///
    /// 연출 흐름:
    ///   1. 슬라이드 인  — 화면 아래 → CenterPos (AnimationCurve 감속)
    ///   2. 부유         — CenterPos에서 Sin 진동 + 이펙트 오버레이 재생
    ///   3. 슬라이드 아웃 — CenterPos → 화면 아래 (AnimationCurve)
    ///
    /// 사용법:
    ///   Project 우클릭 → Create → Game/CutIn/Cutin Sequence Data
    /// </summary>
    [CreateAssetMenu(menuName = "Game/CutIn/Cutin Sequence Data", fileName = "NewCutinSequence")]
    public class CutinSequenceData : ScriptableObject
    {
        // ── 이펙트 오버레이 데이터 ────────────────────────────────────

        /// <summary>
        /// 부유 단계 중 Image 위에 재생할 스프라이트 기반 애니메이션 데이터.
        /// slash, 마법진, 연기 등 어떤 스프라이트 배열도 사용 가능.
        /// </summary>
        [System.Serializable]
        public class CutinEffectData
        {
            [Tooltip("재생할 스프라이트 프레임 배열 (순서대로).")]
            public Sprite[] Frames;

            [Tooltip("초당 프레임 수. OverrideDuration > 0이면 무시됨.")]
            public float Fps = 12f;

            [Tooltip("0보다 크면 이 시간(초) 안에 전체 프레임을 균등 재생.\n" +
                     "0이면 Fps 기준으로 재생.")]
            public float OverrideDuration = 0f;

            [Tooltip("부유 단계 시작 후 이 시간(초)이 지난 뒤 재생을 시작한다.\n" +
                     "여러 이펙트가 있을 경우 앞 이펙트 종료 후 추가로 대기하는 시간이 된다.")]
            public float StartOffsetFromFloat = 0f;

            [Tooltip("이펙트 Image의 localScale 배율. 일러스트 위를 넓게 덮으려면 크게 설정.")]
            public Vector2 Scale = Vector2.one;

            /// <summary>프레임 배열 전체 재생에 걸리는 시간(초).</summary>
            public float TotalDuration
            {
                get
                {
                    if (Frames == null || Frames.Length == 0) return 0f;
                    return OverrideDuration > 0f
                        ? OverrideDuration
                        : Frames.Length / Mathf.Max(0.001f, Fps);
                }
            }
        }

        // ── 일러스트 설정 ─────────────────────────────────────────────

        [Header("일러스트")]
        [Tooltip("화면에 표시할 캐릭터 일러스트 스프라이트.")]
        [SerializeField] private Sprite _illustSprite;

        [Tooltip("일러스트 Image의 localScale 배율. 캐릭터별로 크기를 조정한다.")]
        [SerializeField] private Vector2 _illustScale = Vector2.one;

        [Tooltip("슬라이드 인 완료 후 일러스트가 위치할 anchoredPosition.")]
        [SerializeField] private Vector2 _centerPos = new Vector2(0f, -200f);

        [Tooltip("화면 아래 시작/종료 위치까지의 Y 오프셋(px). 이미지 높이보다 크게 설정.")]
        [SerializeField] private float _offscreenOffsetY = 800f;

        // ── 슬라이드 인 ───────────────────────────────────────────────

        [Header("슬라이드 인")]
        [Tooltip("슬라이드 인 소요 시간(초).")]
        [SerializeField] private float _slideInDuration = 0.4f;

        [Tooltip("슬라이드 인 보간 곡선.\n" +
                 "권장: Ease Out — 시작 빠름 → 도착 직전 확 감속.")]
        [SerializeField] private AnimationCurve _slideInCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3f),
            new Keyframe(1f, 1f, 0f, 0f));

        // ── 부유 ──────────────────────────────────────────────────────

        [Header("부유")]
        [Tooltip("부유 진폭(픽셀). 중앙 위치 기준 위아래 최대 이동 거리.")]
        [SerializeField] private float _floatAmplitude = 12f;

        [Tooltip("부유 주기(Hz). 값이 클수록 빠르게 진동한다.")]
        [SerializeField] private float _floatFrequency = 1.2f;

        [Tooltip("부유 지속 시간(초).")]
        [SerializeField] private float _floatDuration = 1.0f;

        // ── 슬라이드 아웃 ─────────────────────────────────────────────

        [Header("슬라이드 아웃")]
        [Tooltip("슬라이드 아웃 소요 시간(초).\n" +
                 "※ DimFadeOutDuration 이상으로 설정할 것 (암전 페이드 아웃이 먼저 끝나야 함).")]
        [SerializeField] private float _slideOutDuration = 0.3f;

        [Tooltip("슬라이드 아웃 보간 곡선.\n" +
                 "권장: Ease In 또는 Linear.")]
        [SerializeField] private AnimationCurve _slideOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // ── 암전 효과 ─────────────────────────────────────────────────

        [Header("암전 효과")]
        [Tooltip("암전 목표 알파. 0 = 암전 없음, 1 = 완전 암전. 권장: 0.6~0.8.")]
        [SerializeField] private float _dimTargetAlpha = 0.7f;

        [Tooltip("암전 페이드 인 시간(초). 슬라이드 인과 병렬 실행됨.")]
        [SerializeField] private float _dimFadeInDuration = 0.3f;

        [Tooltip("암전 페이드 아웃 시간(초). 슬라이드 아웃과 병렬 실행됨.")]
        [SerializeField] private float _dimFadeOutDuration = 0.25f;

        // ── 이펙트 오버레이 ───────────────────────────────────────────

        [Header("이펙트 오버레이 (부유 중 재생)")]
        [Tooltip("부유 단계 중 일러스트 위에 순차 재생할 스프라이트 애니메이션 목록.\n" +
                 "slash, 마법진 등 종류 무관. 이미지 슬롯이 1개이므로 순차 재생됨.")]
        [SerializeField] private CutinEffectData[] _effects;

        // ── 프로퍼티 ──────────────────────────────────────────────────

        public Sprite         IllustSprite       => _illustSprite;
        public Vector2        IllustScale        => _illustScale;
        public Vector2        CenterPos          => _centerPos;
        public float          OffscreenOffsetY   => _offscreenOffsetY;

        public float          SlideInDuration    => _slideInDuration;
        public AnimationCurve SlideInCurve       => _slideInCurve;

        public float          FloatAmplitude     => _floatAmplitude;
        public float          FloatFrequency     => _floatFrequency;
        public float          FloatDuration      => _floatDuration;

        public float          SlideOutDuration   => _slideOutDuration;
        public AnimationCurve SlideOutCurve      => _slideOutCurve;

        public float          DimTargetAlpha     => _dimTargetAlpha;
        public float          DimFadeInDuration  => _dimFadeInDuration;
        public float          DimFadeOutDuration => _dimFadeOutDuration;

        public CutinEffectData[] Effects         => _effects;

        /// <summary>컷인 전체 총 시간(초). TagTechniqueDefinition.Duration 설정 참고용.</summary>
        public float TotalDuration => _slideInDuration + _floatDuration + _slideOutDuration;
    }
}
