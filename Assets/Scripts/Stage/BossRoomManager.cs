using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// StageBossRoot에 부착.
    /// BossMainController의 이벤트를 받아 보스방 전용 연출을 담당한다.
    ///
    /// 담당 기능:
    ///   - 입장 즉시 좌우 투명벽 활성화 (플레이어 이탈 방지)
    ///   - 보스 첫 조우 연출 (카메라 패닝 → 포효 모션 → 화면 흔들림 → 전투 시작)
    ///   - 보스 사망 시 투명벽 비활성화
    /// </summary>
    public class BossRoomManager : MonoBehaviour
    {
        [Header("카메라")]
        [Tooltip("씬의 메인 CameraFollow 컴포넌트")]
        [SerializeField] private CameraFollow _cameraFollow;

        [Tooltip("카메라가 보스를 바라볼 때 사용하는 추적 속도 (낮을수록 느린 패닝)")]
        [SerializeField] private float _panSpeed = 2.5f;

        [Tooltip("카메라가 보스를 바라보는 시간 (초)")]
        [SerializeField] private float _panDuration = 1.2f;

        [Header("투명벽")]
        [Tooltip("왼쪽 경계 투명벽 오브젝트")]
        [SerializeField] private GameObject _wallLeft;

        [Tooltip("오른쪽 경계 투명벽 오브젝트")]
        [SerializeField] private GameObject _wallRight;

        [Header("보스 포효 연출")]
        [Tooltip("Boss_Head의 SpriteRenderer")]
        [SerializeField] private SpriteRenderer _headRenderer;

        [Tooltip("입이 닫힌 상태부터 최대로 벌린 상태 순 (지옥 보스_0 ~ _4). 총 5장.")]
        [SerializeField] private Sprite[] _roarSprites;

        [Tooltip("포효 사운드")]
        [SerializeField] private AudioClip _roarClip;

        [Tooltip("스프라이트 1장당 표시 시간 (초)")]
        [SerializeField] private float _roarFrameTime = 0.12f;

        [Tooltip("입을 최대로 벌린 채 유지하는 시간 (초)")]
        [SerializeField] private float _roarHoldTime = 1.2f;

        [Header("화면 흔들림")]
        [SerializeField] private float _shakeMagnitude = 0.3f;

        [Header("카메라 줌")]
        [Tooltip("보스방 진입 시 orthographic size (클수록 넓게 보임)")]
        [SerializeField] private float _bossOrthoSize = 7f;

        [Tooltip("줌 전환 시간 (초)")]
        [SerializeField] private float _zoomDuration = 0.8f;

        [Tooltip("전투 중 카메라 중심 Y 오프셋 (보스 Y좌표에 더해짐)")]
        [SerializeField] private float _battleCameraYOffset = 0f;

        [Tooltip("포효 연출 중 카메라 Y 오프셋 추가량 (기본 오프셋에 더해짐)")]
        [SerializeField] private float _bossYOffsetAdd = 2f;

        [Tooltip("카메라 복귀 후 전투 시작까지 대기 시간 (초)")]
        [SerializeField] private float _cameraReturnDelay = 0.8f;

        [Header("플레이어 참조")]
        [Tooltip("플레이어 오브젝트 — 연출 중 입력 차단용")]
        [SerializeField] private PlayerController  _playerController;
        [SerializeField] private PlayerAttack      _playerAttack;
        [SerializeField] private PlayerDash        _playerDash;

        // ── 캐시 ──────────────────────────────────────────────────────────
        private Transform           _bossTransform;
        private BossMainController  _bossCtrl;
        private WaitForSeconds      _waitPan;
        private WaitForSeconds      _waitRoarFrame;
        private WaitForSeconds      _waitRoarHold;
        private WaitForSeconds      _waitCameraReturn;
        private Transform           _mainCamTransform;
        private Vector3             _roomCenter;       // 두 투명벽 사이 중앙 world 좌표
        private float               _roomOrthoSize;    // 투명벽 폭에 맞는 ortho size

        // ── 생명주기 ─────────────────────────────────────────────────────

        private void Awake()
        {
            _bossCtrl         = GetComponentInChildren<BossMainController>();
            _bossTransform    = _bossCtrl?.transform;
            _waitPan          = new WaitForSeconds(_panDuration);
            _waitRoarFrame    = new WaitForSeconds(_roarFrameTime);
            _waitRoarHold     = new WaitForSeconds(_roarHoldTime);
            _waitCameraReturn = new WaitForSeconds(_cameraReturnDelay);
            _mainCamTransform = Camera.main?.transform;
            CalcRoomCamera();
        }

        private void CalcRoomCamera()
        {
            if (_wallLeft == null || _wallRight == null) return;

            float leftX  = _wallLeft.transform.position.x;
            float rightX = _wallRight.transform.position.x;
            float centerX = (leftX + rightX) / 2f;
            float centerY = (_bossTransform != null ? _bossTransform.position.y : 0f) + _battleCameraYOffset;

            _roomCenter = new Vector3(centerX, centerY, 0f);

            // 카메라 가로 반폭 = orthoSize * aspect → orthoSize = halfWidth / aspect
            float halfWidth    = (rightX - leftX) / 2f;
            float aspect       = (float)Screen.width / Screen.height;
            _roomOrthoSize     = halfWidth / aspect;
        }

        private void OnEnable()
        {
            BossMainController.OnBossEngaged += HandleBossEngaged;
            BossMainController.OnBossDead    += HandleBossDead;

            // 입장 즉시 경계벽 활성화
            SetWalls(true);
        }

        private void OnDisable()
        {
            BossMainController.OnBossEngaged -= HandleBossEngaged;
            BossMainController.OnBossDead    -= HandleBossDead;
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────────────

        private void HandleBossEngaged() => StartCoroutine(IntroSequence());

        private void HandleBossDead()
        {
            SetWalls(false);

            if (_cameraFollow != null)
            {
                _cameraFollow.ClearOverrideTarget(); // 인트로 중 사망 시 오버라이드 타깃 안전 해제
                _cameraFollow.RestoreOffset();       // Y 오프셋 복원 (인트로 미완료 대비)
                _cameraFollow.ClearFixedWorldPos();  // 보스방 고정 해제 → 플레이어 추적 복귀
                _cameraFollow.ResetZoom(_zoomDuration); // 줌 부드럽게 복원
            }
        }

        // ── 투명벽 ────────────────────────────────────────────────────────

        private void SetWalls(bool active)
        {
            if (_wallLeft  != null) _wallLeft.SetActive(active);
            if (_wallRight != null) _wallRight.SetActive(active);
        }

        // ── 보스 조우 연출 시퀀스 ─────────────────────────────────────────

        private IEnumerator IntroSequence()
        {
            // ① 플레이어 입력 차단 + 보스 패턴 타이머 정지
            SetPlayerInput(false);
            _bossCtrl?.ApplyActionLock(false);

            // ② 카메라 줌 아웃 + Y 오프셋 상승 + 보스 위치로 패닝 동시 시작
            if (_cameraFollow != null)
            {
                _cameraFollow.ZoomTo(_roomOrthoSize, _zoomDuration);
                _cameraFollow.SetOffsetY(_cameraFollow.OffsetY + _bossYOffsetAdd);
                if (_bossTransform != null)
                    _cameraFollow.SetOverrideTarget(_bossTransform, _panSpeed);
            }

            yield return _waitPan;

            // ③ 포효: 입 벌리기 (0 → 4)
            yield return StartCoroutine(PlayRoarSprites(forward: true));

            // ④ 포효 사운드 + 화면 흔들림 (입 최대 개방 유지)
            if (_roarClip != null && _mainCamTransform != null)
                AudioSource.PlayClipAtPoint(_roarClip, _mainCamTransform.position);

            _cameraFollow?.Shake(_roarHoldTime, _shakeMagnitude);
            yield return _waitRoarHold;

            // ⑤ 입 다물기 (4 → 0)
            yield return StartCoroutine(PlayRoarSprites(forward: false));

            // ⑥ 오프셋 복원 + 방 중앙 고정 모드로 전환
            if (_cameraFollow != null)
            {
                _cameraFollow.RestoreOffset();
                _cameraFollow.ClearOverrideTarget();
                _cameraFollow.SetFixedWorldPos(_roomCenter);
                yield return _waitCameraReturn;
            }

            // ⑦ 보스 패턴 타이머 재개 + 플레이어 입력 복원
            _bossCtrl?.RemoveActionLock(false);
            SetPlayerInput(true);
        }

        // ── 포효 스프라이트 시퀀스 ───────────────────────────────────────

        private IEnumerator PlayRoarSprites(bool forward)
        {
            if (_headRenderer == null || _roarSprites == null || _roarSprites.Length == 0)
                yield break;

            int start = forward ? 0 : _roarSprites.Length - 1;
            int end   = forward ? _roarSprites.Length : -1;
            int step  = forward ? 1 : -1;

            for (int i = start; i != end; i += step)
            {
                if (_roarSprites[i] != null)
                    _headRenderer.sprite = _roarSprites[i];
                yield return _waitRoarFrame;
            }
        }

        // ── 플레이어 입력 제어 ────────────────────────────────────────────

        private void SetPlayerInput(bool enabled)
        {
            if (_playerController != null) _playerController.enabled = enabled;
            if (_playerAttack     != null) _playerAttack.enabled     = enabled;
            if (_playerDash       != null) _playerDash.enabled       = enabled;
        }
    

/// <summary>छठ레이저 페이즈 진입 시 입 벌리기 애니메이션을 외부에서 시작한다.</summary>
        public Coroutine OpenMouth()  => StartCoroutine(PlayRoarSprites(forward: true));

        /// <summary>레이저 페이즈 종료 시 입 다물기 애니메이션을 외부에서 시작한다.</summary>
        public Coroutine CloseMouth() => StartCoroutine(PlayRoarSprites(forward: false));
}
}
