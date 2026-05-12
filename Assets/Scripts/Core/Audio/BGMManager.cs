using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class BGMManager : MonoBehaviour
    {
        public static BGMManager Instance { get; private set; }

        [SerializeField] private float _crossFadeDuration = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float _volume = 1.0f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool        _isAActive;
        private Coroutine   _fadeCoroutine;

        private AudioSource Current => _isAActive ? _sourceA : _sourceB;
        private AudioSource Next    => _isAActive ? _sourceB : _sourceA;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            _sourceA = CreateSource("BGM_A");
            _sourceB = CreateSource("BGM_B");
            _isAActive = true;
        }

        private AudioSource CreateSource(string goName)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop         = true;
            src.playOnAwake  = false;
            src.volume       = 0f;
            src.spatialBlend = 0f;
            return src;
        }

        /// <summary>
        /// BGM 재생. clip이 null이면 정지, 현재와 동일한 클립이면 무시.
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null)
            {
                StopBGM();
                return;
            }

            if (Current.clip == clip && Current.isPlaying) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossFade(clip));
        }

        public void StopBGM()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut(Current));
        }

        // ── 내부 코루틴 ───────────────────────────────────────────────────

        private IEnumerator CrossFade(AudioClip clip)
        {
            var fadeOut = Current;
            var fadeIn  = Next;

            fadeIn.clip   = clip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            float elapsed = 0f;
            while (elapsed < _crossFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / _crossFadeDuration;
                fadeOut.volume = Mathf.Lerp(_volume, 0f, t);
                fadeIn.volume  = Mathf.Lerp(0f, _volume, t);
                yield return null;
            }

            fadeOut.volume = 0f;
            fadeOut.Stop();
            fadeOut.clip = null;

            fadeIn.volume  = _volume;
            _isAActive     = !_isAActive;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOut(AudioSource src)
        {
            float start   = src.volume;
            float elapsed = 0f;
            while (elapsed < _crossFadeDuration)
            {
                elapsed   += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, 0f, elapsed / _crossFadeDuration);
                yield return null;
            }
            src.volume = 0f;
            src.Stop();
            src.clip = null;
            _fadeCoroutine = null;
        }
    }
}
