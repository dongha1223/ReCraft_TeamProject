using System;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스킬 이펙트 전용 프레임 애니메이터.
    /// Inspector에서 Sprite[]를 순서대로 연결하면 지정 FPS로 재생 후 자동 파괴된다.
    ///
    /// OverrideDuration > 0 이면 FPS 대신 총 재생 시간을 기준으로
    /// 프레임 간격을 자동 계산한다 (interval = OverrideDuration / frames.Length).
    ///
    /// 프리팹 구성:
    ///   - SpriteRenderer (Order in Layer를 적절히 설정)
    ///   - 이 컴포넌트 (SkillEffectActor)
    ///
    /// 사용법:
    ///   Instantiate(_effectPrefab, origin, Quaternion.identity)
    ///   — 풀 사용 시 OnEnable에서 자동 초기화됨
    /// </summary>
    public class SkillEffectActor : MonoBehaviour
    {
        [Tooltip("재생할 스프라이트 배열 (프레임 순서대로 연결)")]
        [SerializeField] private Sprite[] _frames;

        [Tooltip("초당 프레임 수. OverrideDuration이 0보다 크면 무시된다.")]
        [SerializeField] private float _fps = 12f;

        [Tooltip("0보다 크면 이 시간(초) 안에 모든 프레임을 균등하게 재생한다.\n" +
                 "0이면 FPS 기준으로 재생.")]
        [SerializeField] private float _overrideDuration = 0f;

        [Tooltip("이펙트 크기. 박스 크기(BoxSize)에 맞게 조정한다.")]
        [SerializeField] private Vector2 _scale = Vector2.one;

        private SpriteRenderer _renderer;
        private float          _timer;
        private int            _frameIndex;
        private bool           _playing;
        private float          _frameInterval; // OnEnable에서 한 번만 계산

        /// <summary>
        /// 실제 총 재생 시간(초).
        /// OverrideDuration > 0이면 그 값, 아니면 frames.Length / fps.
        /// </summary>
        public float TotalDuration
        {
            get
            {
                if (_frames == null || _frames.Length == 0) return 0f;
                return _overrideDuration > 0f
                    ? _overrideDuration
                    : _frames.Length / Mathf.Max(0.001f, _fps);
            }
        }

        /// <summary>재생이 완전히 끝났을 때 한 번 발행된다. Destroy 직전에 호출.</summary>
        public event Action OnCompleted;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _timer      = 0f;
            _frameIndex = 0;
            _playing    = _frames != null && _frames.Length > 0;

            // 프레임 간격을 OnEnable에서 한 번 계산해 Update 부하 절감
            if (_playing)
            {
                _frameInterval = _overrideDuration > 0f
                    ? _overrideDuration / _frames.Length
                    : 1f / Mathf.Max(0.001f, _fps);
            }

            // 지정 스케일 적용
            transform.localScale = new Vector3(_scale.x, _scale.y, 1f);

            // 첫 프레임 즉시 표시
            if (_playing && _renderer != null)
                _renderer.sprite = _frames[0];
        }

        private void Update()
        {
            if (!_playing) return;

            _timer += Time.deltaTime;

            while (_timer >= _frameInterval)
            {
                _timer -= _frameInterval;
                _frameIndex++;

                if (_frameIndex >= _frames.Length)
                {
                    _playing = false;
                    OnCompleted?.Invoke();
                    Destroy(gameObject);
                    return;
                }

                if (_renderer != null)
                    _renderer.sprite = _frames[_frameIndex];
            }
        }
    }
}
