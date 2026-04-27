using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _2D_Roguelike
{
    /// <summary>
    /// 태그기 컷인 일러스트 연출 재생기.
    ///
    /// 연출 흐름:
    ///   1. 슬라이드 인  — 화면 아래 → CenterPos (AnimationCurve 감속)
    ///                     암전 페이드 인 병렬 실행
    ///   2. 부유         — CenterPos에서 Sin 진동
    ///                     이펙트 오버레이 순차 재생 (병렬)
    ///   3. 슬라이드 아웃 — CenterPos → 화면 아래 (AnimationCurve)
    ///                     암전 페이드 아웃 병렬 실행
    ///
    /// Inspector 구성:
    ///   Canvas (Screen Space - Overlay) 하위에 세 Image를 배치한다.
    ///   Sibling 순서: DimOverlay(뒤) → IllustImage → EffectImage(앞)
    ///   위치·크기·이펙트 설정은 CutinSequenceData SO에서 제어한다.
    ///
    /// 사용법:
    ///   yield return StartCoroutine(cutinPlayer.Play(data));
    /// </summary>
    public class CutinIllustPlayer : MonoBehaviour
    {
        [Header("UI 참조 (Inspector에서 연결)")]
        [Tooltip("일러스트를 표시할 Image 컴포넌트.")]
        [SerializeField] private Image _illustImage;

        [Tooltip("암전용 전체 화면 Image. DimOverlay GameObject의 Image 연결.\n" +
                 "null이면 암전 효과를 건너뜀.")]
        [SerializeField] private Image _dimImage;

        [Tooltip("이펙트 오버레이용 Image. IllustImage 위 sibling에 배치.\n" +
                 "null이면 이펙트 재생을 건너뜀.")]
        [SerializeField] private Image _effectImage;

        // _illustImage와 같은 오브젝트의 RectTransform (캐시)
        private RectTransform _illustRect;

        private void Awake()
        {
            if (_illustImage != null)
                _illustRect = _illustImage.GetComponent<RectTransform>();
        }

        // ── 공개 실행 ─────────────────────────────────────────────────

        /// <summary>
        /// 컷인 연출을 처음부터 끝까지 재생한다.
        /// yield return으로 호출하면 연출이 완전히 끝난 뒤 다음 줄로 이동한다.
        /// </summary>
        public IEnumerator Play(CutinSequenceData data)
        {
            if (data == null || _illustImage == null || _illustRect == null)
                yield break;

            // 일러스트 초기화
            _illustImage.sprite  = data.IllustSprite;
            _illustImage.enabled = true;
            _illustRect.localScale       = new Vector3(data.IllustScale.x, data.IllustScale.y, 1f);
            _illustRect.anchoredPosition = data.CenterPos + new Vector2(0f, -data.OffscreenOffsetY);

            // 1. 슬라이드 인 + 암전 페이드 인 (병렬)
            if (data.DimTargetAlpha > 0f && _dimImage != null)
                StartCoroutine(FadeDim(0f, data.DimTargetAlpha, data.DimFadeInDuration));

            yield return SlideIn(data);

            if (_illustImage == null) yield break;

            // 2. 부유 + 이펙트 오버레이 (병렬)
            yield return Float(data);

            if (_illustImage == null) yield break;

            // 3. 슬라이드 아웃 + 암전 페이드 아웃 (병렬)
            if (data.DimTargetAlpha > 0f && _dimImage != null)
                StartCoroutine(FadeDim(data.DimTargetAlpha, 0f, data.DimFadeOutDuration));

            yield return SlideOut(data);

            // 정리
            _illustImage.enabled = false;
            if (_dimImage != null)   _dimImage.enabled   = false;
            if (_effectImage != null) _effectImage.enabled = false;
        }

        // ── 슬라이드 인 ───────────────────────────────────────────────

        private IEnumerator SlideIn(CutinSequenceData data)
        {
            Vector2 from     = data.CenterPos + new Vector2(0f, -data.OffscreenOffsetY);
            Vector2 to       = data.CenterPos;
            float   elapsed  = 0f;
            float   duration = data.SlideInDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float curvedT = data.SlideInCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                _illustRect.anchoredPosition = Vector2.LerpUnclamped(from, to, curvedT);
                yield return null;
            }

            _illustRect.anchoredPosition = to;
        }

        // ── 부유 ──────────────────────────────────────────────────────

        private IEnumerator Float(CutinSequenceData data)
        {
            // 이펙트 오버레이를 병렬로 시작
            if (data.Effects != null && data.Effects.Length > 0 && _effectImage != null)
                StartCoroutine(EffectSequence(data.Effects));

            float elapsed   = 0f;
            float duration  = data.FloatDuration;
            float amplitude = data.FloatAmplitude;
            float frequency = data.FloatFrequency;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f) * amplitude;
                _illustRect.anchoredPosition = data.CenterPos + new Vector2(0f, offset);
                yield return null;
            }

            _illustRect.anchoredPosition = data.CenterPos;
        }

        // ── 슬라이드 아웃 ─────────────────────────────────────────────

        private IEnumerator SlideOut(CutinSequenceData data)
        {
            Vector2 from     = data.CenterPos;
            Vector2 to       = data.CenterPos + new Vector2(0f, -data.OffscreenOffsetY);
            float   elapsed  = 0f;
            float   duration = data.SlideOutDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float curvedT = data.SlideOutCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                _illustRect.anchoredPosition = Vector2.LerpUnclamped(from, to, curvedT);
                yield return null;
            }
        }

        // ── 암전 ──────────────────────────────────────────────────────

        private IEnumerator FadeDim(float fromAlpha, float toAlpha, float duration)
        {
            if (_dimImage == null) yield break;

            _dimImage.enabled = true;
            Color c = _dimImage.color;

            if (duration <= 0f)
            {
                c.a = toAlpha;
                _dimImage.color   = c;
                _dimImage.enabled = toAlpha > 0f;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
                _dimImage.color = c;
                yield return null;
            }

            c.a = toAlpha;
            _dimImage.color   = c;
            _dimImage.enabled = toAlpha > 0f;
        }

        // ── 이펙트 오버레이 ───────────────────────────────────────────

        /// <summary>
        /// Effects 배열을 순차 재생한다.
        /// 각 이펙트는 StartOffsetFromFloat 이후에 시작되며,
        /// 앞 이펙트 종료 시점 기준으로 오프셋이 추가 적용된다.
        /// </summary>
        private IEnumerator EffectSequence(CutinSequenceData.CutinEffectData[] effects)
        {
            foreach (var effect in effects)
            {
                if (effect == null || effect.Frames == null || effect.Frames.Length == 0)
                    continue;

                if (effect.StartOffsetFromFloat > 0f)
                    yield return new WaitForSeconds(effect.StartOffsetFromFloat);

                yield return PlaySingleEffect(effect);
            }

            if (_effectImage != null)
                _effectImage.enabled = false;
        }

        /// <summary>
        /// 단일 이펙트 데이터의 프레임 배열을 Image에 재생한다.
        /// SkillEffectActor와 동일한 프레임 간격 로직을 사용한다.
        /// </summary>
        private IEnumerator PlaySingleEffect(CutinSequenceData.CutinEffectData effect)
        {
            if (_effectImage == null) yield break;

            // 스케일 적용
            _effectImage.rectTransform.localScale =
                new Vector3(effect.Scale.x, effect.Scale.y, 1f);

            _effectImage.enabled = true;
            _effectImage.sprite  = effect.Frames[0];

            float interval = effect.OverrideDuration > 0f
                ? effect.OverrideDuration / effect.Frames.Length
                : 1f / Mathf.Max(0.001f, effect.Fps);

            float timer      = 0f;
            int   frameIndex = 0;

            while (true)
            {
                timer += Time.deltaTime;

                while (timer >= interval)
                {
                    timer -= interval;
                    frameIndex++;

                    if (frameIndex >= effect.Frames.Length)
                    {
                        _effectImage.enabled = false;
                        yield break;
                    }

                    _effectImage.sprite = effect.Frames[frameIndex];
                }

                yield return null;
            }
        }
    }
}
