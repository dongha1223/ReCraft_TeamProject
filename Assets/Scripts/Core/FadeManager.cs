using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스테이지 전환 Fade in/out 및 Game Clear UI 제어
    /// GameClearUI.uxml 이 할당된 UIDocument 컴포넌트가 같은 GameObject에 있어야 함
    /// </summary>
    public class FadeManager : MonoBehaviour
    {
        public static FadeManager Instance { get; private set; }

        [SerializeField] private float _fadeDuration = 0.8f;

        private VisualElement _fadeOverlay;
        private VisualElement _gameclearOverlay;
        private Label         _countdownLabel;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _fadeOverlay      = root.Q<VisualElement>("fade-overlay");
            _gameclearOverlay = root.Q<VisualElement>("gameclear-overlay");
            _countdownLabel   = root.Q<Label>("countdown-label");

            // 초기 상태: 페이드 오버레이 투명, 클리어 화면 숨김
            _fadeOverlay.style.opacity = 0f;
            _gameclearOverlay.style.display = DisplayStyle.None;
        }

        // ── GameClear UI ────────────────────────────────────────────────
        public void ShowGameClear(bool show)
        {
            _gameclearOverlay.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetCountdown(int seconds)
        {
            if (_countdownLabel == null) return;
            _countdownLabel.text = seconds > 0 ? $"{seconds}초 후 스테이지 1로 돌아갑니다..." : "";
        }

        // ── Fade 코루틴 ─────────────────────────────────────────────────
        /// <summary>투명 → 검은 화면</summary>
        public IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.style.opacity = Mathf.Lerp(0f, 1f, elapsed / _fadeDuration);
                yield return null;
            }
            _fadeOverlay.style.opacity = 1f;
        }

        /// <summary>검은 화면 → 투명</summary>
        public IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.style.opacity = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
                yield return null;
            }
            _fadeOverlay.style.opacity = 0f;
        }
    }
}
