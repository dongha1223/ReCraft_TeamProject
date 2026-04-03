using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 미니맵 전용 카메라를 제어합니다.
    /// "Minimap" 레이어만 culling하여 렌더링하므로 메인 게임 비주얼과 완전히 분리됩니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCameraController : MonoBehaviour
    {
        [SerializeField] private float _orthographicSize = 10f;

        [SerializeField] private Transform _target;
        private Camera _minimapCamera;

        private void Awake()
        {
            _minimapCamera = GetComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = _orthographicSize;
            // Minimap 레이어만 렌더링
            
            
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) _target = playerGO.transform;
                return;
            }

            // x/y 추적, z는 고정
            transform.position = new Vector3(_target.position.x, _target.position.y, -10f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(transform.position,
                new Vector3(_orthographicSize * 4f, _orthographicSize * 2f, 0f));
        }
#endif
    }
}