using UnityEngine;

namespace _2D_Roguelike
{
    // CameraFollow(-10)보다 나중에 실행되어야 카메라 이동량을 올바르게 읽음
    [DefaultExecutionOrder(10)]
    public class ParallaxLayer : MonoBehaviour
    {
        // 0: 카메라와 함께 이동(화면에 고정, 앞 레이어), 1: 월드에 고정(빠른 스크롤)
        // 뒷배경일수록 작은 값(예: 0.1), 앞배경일수록 큰 값(예: 0.9)
        [SerializeField, Range(0f, 1f)] private float _parallaxFactorX = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _parallaxFactorY = 0.05f;

        [SerializeField] private Transform _leftCopy;
        [SerializeField] private Transform _rightCopy;

        private Transform _cameraTransform;
        private float _spriteWidth;
        private bool _hasInfiniteScroll;

        // (1 - factor) 미리 계산 — LateUpdate에서 매 프레임 연산 절약
        private float _mulX;
        private float _mulY;

        private Vector3 _cameraStartPos;
        private Vector3 _startPos;

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _cameraStartPos  = _cameraTransform.position;
            _startPos        = transform.position;

            CacheMultipliers();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                _spriteWidth = sr.bounds.size.x;

            _hasInfiniteScroll = _leftCopy != null && _rightCopy != null && _spriteWidth > 0f;
        }

        private void LateUpdate()
        {
            Vector3 camPos   = _cameraTransform.position;
            float   targetX  = _startPos.x + (camPos.x - _cameraStartPos.x) * _mulX;
            float   targetY  = _startPos.y + (camPos.y - _cameraStartPos.y) * _mulY;
            float   startZ   = _startPos.z;

            transform.position = new Vector3(targetX, targetY, startZ);

            if (!_hasInfiniteScroll) return;

            _leftCopy.position  = new Vector3(targetX - _spriteWidth, targetY, startZ);
            _rightCopy.position = new Vector3(targetX + _spriteWidth, targetY, startZ);

            float diffX = targetX - camPos.x;
            if (diffX > _spriteWidth)
                _startPos.x -= _spriteWidth;
            else if (diffX < -_spriteWidth)
                _startPos.x += _spriteWidth;
        }

        private void CacheMultipliers()
        {
            _mulX = 1f - _parallaxFactorX;
            _mulY = 1f - _parallaxFactorY;
        }

#if UNITY_EDITOR
        // Inspector에서 factor 조정 시 multiplier 즉시 반영
        private void OnValidate() => CacheMultipliers();
#endif
    }
}
