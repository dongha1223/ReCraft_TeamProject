using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    public class StartSceneController : MonoBehaviour
    {
        // 페이드 인/아웃 지속 시간 (초)
        [SerializeField] private float _fadeDuration = 1.5f;

        // 점멸 간격 (초)
        [SerializeField] private float _blinkInterval = 0.5f;

        // 전환할 인게임 씬 이름
        [SerializeField] private string _inGameSceneName = "InGameScene";

        private VisualElement _fadeOverlay;
        private Label _promptLabel;
        private bool _inputEnabled = false;
        private Coroutine _blinkCoroutine;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _fadeOverlay = root.Q<VisualElement>("fade-overlay");
            _promptLabel = root.Q<Label>("prompt-label");

            // 시작 시 프롬프트 숨김
            _promptLabel.style.opacity = 0f;

            StartCoroutine(FadeIn());
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            // 아무 키 입력 감지 (New Input System) - 키보드 및 마우스 클릭 포함
            bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            if (keyPressed || mousePressed)
            {
                OnAnyKeyPressed();
            }
        }

        private void OnAnyKeyPressed()
        {
            if (!_inputEnabled) return;
            _inputEnabled = false;

            if (_blinkCoroutine != null)
                StopCoroutine(_blinkCoroutine);

            StartCoroutine(FadeOutAndLoadScene());
        }

        /// <summary>
        /// 검은 오버레이를 opacity 1 → 0 으로 페이드 인
        /// </summary>
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.style.opacity = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
                yield return null;
            }
            _fadeOverlay.style.opacity = 0f;

            // 페이드 완료 후 점멸 시작 및 입력 활성화
            _blinkCoroutine = StartCoroutine(BlinkPrompt());
            _inputEnabled = true;
        }

        /// <summary>
        /// 프롬프트 텍스트를 opacity 0 ↔ 1 로 반복 점멸
        /// </summary>
        private IEnumerator BlinkPrompt()
        {
            while (true)
            {
                _promptLabel.style.opacity = 1f;
                yield return new WaitForSeconds(_blinkInterval);
                _promptLabel.style.opacity = 0f;
                yield return new WaitForSeconds(_blinkInterval);
            }
        }

        /// <summary>
        /// 오버레이를 opacity 0 → 1 로 페이드 아웃 후 씬 전환
        /// </summary>
        private IEnumerator FadeOutAndLoadScene()
        {
            _promptLabel.style.opacity = 0f;

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.style.opacity = Mathf.Lerp(0f, 1f, elapsed / _fadeDuration);
                yield return null;
            }
            _fadeOverlay.style.opacity = 1f;

            SceneManager.LoadScene(_inGameSceneName);
        }
    }
}
