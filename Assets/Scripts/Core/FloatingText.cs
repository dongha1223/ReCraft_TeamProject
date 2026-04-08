using System;
using UnityEngine;
using TMPro;

namespace _2D_Roguelike
{
    /// <summary>
    /// 위로 튀어오른 뒤 중력으로 낙하하며 페이드아웃되는 플로팅 텍스트
    /// FloatingTextSpawner의 ObjectPool에서 관리됨
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("운동")]
        [SerializeField] private float _initialUpSpeed  = 4f;
        [SerializeField] private float _gravity         = 10f;
        [SerializeField] private float _horizontalDrift = 1.2f;  // 좌우 랜덤 드리프트 최대값

        [Header("페이드")]
        [SerializeField] private float _fadeDelay  = 0.25f;  // 이 시간 이후부터 페이드 시작
        [SerializeField] private float _fadeSpeed  = 2.5f;
        [SerializeField] private float _lifetime   = 1.1f;

        private TextMeshPro            _tmp;
        private float                  _velocityY;
        private float                  _velocityX;
        private float                  _elapsed;
        private Color                  _color;
        private Action<FloatingText>   _onExpired;  // 풀 반환 콜백 (FloatingTextSpawner에서 주입)

        private void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
        }

        /// <summary>풀에서 꺼낸 뒤 초기화</summary>
        public void Init(string text, Color color, Action<FloatingText> onExpired)
        {
            _tmp.text   = text;
            _color      = color;
            _color.a    = 1f;
            _tmp.color  = _color;

            _velocityY  = _initialUpSpeed;
            _velocityX  = UnityEngine.Random.Range(-_horizontalDrift, _horizontalDrift);
            _elapsed    = 0f;
            _onExpired  = onExpired;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // 포물선 운동 (초기 상향 속도 + 중력 감속)
            _velocityY -= _gravity * Time.deltaTime;
            transform.position += new Vector3(_velocityX, _velocityY, 0f) * Time.deltaTime;

            // fadeDelay 이후 서서히 투명해짐
            if (_elapsed > _fadeDelay)
            {
                _color.a   = Mathf.Max(0f, _color.a - _fadeSpeed * Time.deltaTime);
                _tmp.color = _color;
            }

            if (_elapsed >= _lifetime)
            {
                _onExpired?.Invoke(this);
            }
        }

        // ── 풀 이벤트 ────────────────────────────────────────────────
        public void OnGetFromPool()  => gameObject.SetActive(true);
        public void OnReturnToPool() => gameObject.SetActive(false);
    }
}
