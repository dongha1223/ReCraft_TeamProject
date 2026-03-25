using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _2D_Roguelike
{
    /// <summary>
    /// 미니맵 전용 카메라 컨트롤러
    ///
    /// 기능:
    ///   - 플레이어를 자동 추적 (LateUpdate)
    ///   - Orthographic 모드로 고정
    ///   - "Minimap" 레이어만 렌더링하도록 cullingMask 설정
    ///   - RenderTexture를 타겟으로 출력
    ///
    /// 수정 사항:
    ///   - cullingMask 설정 코드 누락 → 완성
    ///   - 레이어 미존재 시 안전 처리
    ///   - URP Stack 카메라 설정 자동화
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCameraController : MonoBehaviour
    {
        [Header("카메라 설정")]
        [SerializeField] private float _orthographicSize = 10f;
        [SerializeField] private RenderTexture _renderTexture;

        [Header("추적 대상 (비워두면 Player 태그로 자동 탐색)")]
        [SerializeField] private Transform _target;

        private Camera _minimapCamera;

        private void Awake()
        {
            _minimapCamera = GetComponent<Camera>();

            // ── Orthographic 설정 ─────────────────────────────────────
            _minimapCamera.orthographic     = true;
            _minimapCamera.orthographicSize = _orthographicSize;
            _minimapCamera.nearClipPlane    = -20f;
            _minimapCamera.farClipPlane     =  20f;

            // ── cullingMask: Minimap 레이어만 렌더링 ─────────────────
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer >= 0)
            {
                _minimapCamera.cullingMask = 1 << minimapLayer;
                Debug.Log($"[MinimapCamera] cullingMask = Minimap(Layer {minimapLayer})");
            }
            else
            {
                Debug.LogWarning("[MinimapCamera] 'Minimap' 레이어를 찾을 수 없습니다. Tags & Layers 설정 확인 필요.");
                // 폴백: Default + Minimap 근처 레이어 6번을 직접 사용
                _minimapCamera.cullingMask = 1 << 6;
            }

            // ── RenderTexture 연결 ────────────────────────────────────
            if (_renderTexture != null)
            {
                _minimapCamera.targetTexture = _renderTexture;
            }
            else
            {
                Debug.LogWarning("[MinimapCamera] RenderTexture가 할당되지 않았습니다. Inspector에서 MinimapRT를 연결하세요.");
            }

            // ── URP: Overlay 카메라로 설정 ────────────────────────────
            var urd = GetComponent<UniversalAdditionalCameraData>();
            if (urd != null)
            {
                urd.renderType = CameraRenderType.Base;
                urd.renderShadows = false;
            }
        }

        private void LateUpdate()
        {
            // 타겟 자동 탐색
            if (_target == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) _target = playerGO.transform;
                return;
            }

            // 플레이어 위치 추적 (z 고정)
            transform.position = new Vector3(
                _target.position.x,
                _target.position.y,
                -10f
            );
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
