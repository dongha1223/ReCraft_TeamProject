using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 스킬 이펙트 전용 프레임 애니메이터.
    /// Inspector에서 Sprite[]를 순서대로 연결하면 지정 FPS로 재생 후 자동 파괴된다.
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

        [Tooltip("초당 프레임 수")]
        [SerializeField] private float _fps = 12f;

        [Tooltip("이펙트 크기. 박스 크기(BoxSize)에 맞게 조정한다.")]
        [SerializeField] private Vector2 _scale = Vector2.one;

        private SpriteRenderer _renderer;
        private float          _timer;
        private int            _frameIndex;
        private bool           _playing;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _timer      = 0f;
            _frameIndex = 0;
            _playing    = _frames != null && _frames.Length > 0;

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
            float interval = 1f / Mathf.Max(0.001f, _fps);

            while (_timer >= interval)
            {
                _timer -= interval;
                _frameIndex++;

                if (_frameIndex >= _frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }

                if (_renderer != null)
                    _renderer.sprite = _frames[_frameIndex];
            }
        }
    }
}
