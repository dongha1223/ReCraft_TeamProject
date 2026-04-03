using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어 대시
    /// - 최대 2회 충전, 첫 소모 시 쿨타임 타이머 시작 → 완료 시 2회 복구
    /// - 대시 중 재대시 불가 (_isDashing이 감속 구간까지 true 유지)
    /// - 점프 후 _jumpLockDuration 동안 대시 불가
    /// - 대시 중 중력 제거, 감속 종료 후 _postDashHangTime 동안 중력 유지 제거
    /// - 감속 구간에서 이동 입력 시 즉시 해제
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerDash : MonoBehaviour
    {
        [Header("대시 기본")]
        [SerializeField] private float _dashSpeed        = 18f;
        [SerializeField] private float _dashDuration     = 0.15f;
        [SerializeField] private int   _maxCharges       = 2;
        [SerializeField] private float _dashCooldown     = 1f;     // 첫 소모 후 최대 충전까지 시간

        [Header("조작감")]
        [SerializeField] private float _decelerationTime  = 0.10f; // 대시 종료 후 감쇠 시간 (대시 상태 포함)
        [SerializeField] private float _jumpLockDuration  = 0.25f; // 점프 후 대시 잠금 시간
        [SerializeField] private float _postDashHangTime  = 0.20f; // 감속 후 공중 정지 시간 (중력 지연 복원)

        private Rigidbody2D _rb;
        private Animator    _animator;

        private int   _currentCharges;
        private bool  _isDashing;
        private bool  _cooldownRunning;
        private float _jumpLockTimer;
        private float _originalGravityScale;

        private static readonly int AnimDash = Animator.StringToHash("Dash");

        public bool IsDashing => _isDashing;

        public GhostFade ghost;

        private void Awake()
        {
            _rb                   = GetComponent<Rigidbody2D>();
            _animator             = GetComponent<Animator>();
            _currentCharges       = _maxCharges;
            _originalGravityScale = _rb.gravityScale;
        }

        private void Update()
        {
            // 점프 잠금 타이머 감소
            if (_jumpLockTimer > 0f)
                _jumpLockTimer -= Time.deltaTime;

            // 대시 중(감속 포함)에는 입력 차단
            if (_isDashing) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool canDash = _currentCharges > 0 && _jumpLockTimer <= 0f;
            if (keyboard.zKey.wasPressedThisFrame && canDash)
                StartCoroutine(DashCoroutine());
        }

        /// <summary>
        /// 점프 시 PlayerController에서 호출 — 대시 잠금 시작
        /// </summary>
        public void OnJump()
        {
            _jumpLockTimer = _jumpLockDuration;
        }

        private IEnumerator DashCoroutine()
        {
            // 충전 소모, 처음 소모되는 순간에만 쿨타임 타이머 시작
            _currentCharges--;
            if (!_cooldownRunning)
                StartCoroutine(RechargeCoroutine());

            _isDashing      = true;
            ghost.makeGhost = true;

            // 중력 제거 + Y 속도 초기화 (수평 대시 보장)
            _rb.gravityScale      = 0f;

            float dir = transform.localScale.x < 0f ? -1f : 1f;
            _animator?.SetTrigger(AnimDash);
            _rb.linearVelocity = new Vector2(dir * _dashSpeed, 0f);

            yield return new WaitForSeconds(_dashDuration);

            // ── 감속 구간 (_isDashing = true 유지, 새 대쉬 입력 차단) ──────────
            ghost.makeGhost = false;

            float elapsed   = 0f;
            float startVelX = _rb.linearVelocity.x;

            while (elapsed < _decelerationTime)
            {
                // 이동 입력 감지 시 즉시 감쇠 종료 → HandleMovement가 이어받음
                var keyboard = Keyboard.current;
                if (keyboard != null &&
                    (keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed))
                    break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _decelerationTime);
                _rb.linearVelocity = new Vector2(Mathf.Lerp(startVelX, 0f, t), _rb.linearVelocity.y);
                yield return null;
            }

            // ── 대시 상태 종료, 다음 대시 입력 가능 ────────────────────────────
            _isDashing = false;

            // ── 공중 행 타임: 중력 없이 잠시 정지 (지상이면 무의미하지만 무해) ──
            if (_postDashHangTime > 0f)
                yield return new WaitForSeconds(_postDashHangTime);

            // 중력 복원
            _rb.gravityScale = 3;
        }

        /// <summary>
        /// 첫 소모 시 시작, 완료 시 충전 최대 복구
        /// </summary>
        private IEnumerator RechargeCoroutine()
        {
            _cooldownRunning = true;
            yield return new WaitForSeconds(_dashCooldown);
            _currentCharges  = _maxCharges;
            _cooldownRunning = false;
        }
    }
}
