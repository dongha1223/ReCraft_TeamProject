using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    // ParallaxLayer보다 반드시 먼저 실행되어야 카메라 delta가 정확히 전달됨
    [DefaultExecutionOrder(-10)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothSpeed = 6f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, -10f);

        private Transform _overrideTarget;
        private float     _overrideSmoothSpeed = -1f; // -1 = _smoothSpeed 그대로
        private Vector3   _shakeOffset;
        private Coroutine _shakeCoroutine;
        private Coroutine _zoomCoroutine;
        private float     _defaultOrthoSize;
        private Vector3   _defaultOffset;
        private Camera    _cam;
        private bool      _useFixedPos;
        private Vector3   _fixedWorldPos;

        // ── 외부 API ─────────────────────────────────────────────────────

        /// <summary>
        /// 카메라가 기본 타깃 대신 지정 Transform을 추적하게 한다.
        /// smoothSpeed를 0 이하로 주면 기본 _smoothSpeed를 사용.
        /// </summary>
        public void SetOverrideTarget(Transform target, float smoothSpeed = -1f)
        {
            _overrideTarget      = target;
            _overrideSmoothSpeed = smoothSpeed;
        }

        /// <summary>카메라를 기본 타깃 추적 모드로 되돌린다.</summary>
        public void ClearOverrideTarget()
        {
            _overrideTarget      = null;
            _overrideSmoothSpeed = -1f;
        }

        public float OffsetY => _offset.y;

        /// <summary>오프셋 Y값을 임시로 덮어쓴다. RestoreOffset으로 복원.</summary>
        public void SetOffsetY(float y) => _offset.y = y;

        /// <summary>오프셋을 Inspector에서 설정한 원래 값으로 복원한다.</summary>
        public void RestoreOffset() => _offset = _defaultOffset;

        /// <summary>카메라를 world 좌표 고정 위치로 전환한다. SetOverrideTarget보다 우선.</summary>
        public void SetFixedWorldPos(Vector3 worldPos) { _fixedWorldPos = worldPos; _useFixedPos = true; }

        /// <summary>고정 위치 모드를 해제하고 타깃 추적으로 복귀한다.</summary>
        public void ClearFixedWorldPos() => _useFixedPos = false;

        // ── 외부 API (줌) ────────────────────────────────────────────────

        /// <summary>화면을 duration 동안 magnitude 세기로 흔든다.</summary>
        public void Shake(float duration, float magnitude)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        // ── 외부 API (줌) ────────────────────────────────────────────────

        /// <summary>카메라 orthographic size를 duration 동안 부드럽게 변경한다.</summary>
        public void ZoomTo(float targetSize, float duration)
        {
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(ZoomCoroutine(targetSize, duration));
        }

        /// <summary>카메라 줌을 Awake 시 기본값으로 복귀한다.</summary>
        public void ResetZoom(float duration) => ZoomTo(_defaultOrthoSize, duration);

        // ── 내부 ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _cam           = GetComponent<Camera>();
            _defaultOffset = _offset;
            if (_cam != null) _defaultOrthoSize = _cam.orthographicSize;
        }

        private void LateUpdate()
        {
            Vector3 desired;

            if (_useFixedPos)
            {
                desired   = _fixedWorldPos + _offset;
                desired.z = _offset.z;
                transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime) + _shakeOffset;
                return;
            }

            Transform followTarget = _overrideTarget != null ? _overrideTarget : _target;

            if (followTarget == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) _target = playerGO.transform;
                return;
            }

            float speed = (_overrideTarget != null && _overrideSmoothSpeed >= 0f)
                        ? _overrideSmoothSpeed
                        : _smoothSpeed;

            desired   = followTarget.position + _offset;
            desired.z = _offset.z;

            transform.position = Vector3.Lerp(transform.position, desired, speed * Time.deltaTime) + _shakeOffset;
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float remaining = 1f - (elapsed / duration);
                _shakeOffset = (Vector3)Random.insideUnitCircle * magnitude * remaining;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _shakeOffset    = Vector3.zero;
            _shakeCoroutine = null;
        }

        private IEnumerator ZoomCoroutine(float targetSize, float duration)
        {
            if (_cam == null) yield break;

            float startSize = _cam.orthographicSize;
            float elapsed   = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
                yield return null;
            }

            _cam.orthographicSize = targetSize;
            _zoomCoroutine        = null;
        }
    }
}
