using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("점프")]
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private int _maxJumpCount = 2;

        [Header("발 감지")]
        [SerializeField] private Vector2 _feetOffset  = new Vector2(0f, -0.32f);
        [SerializeField] private float _feetWidth     = 0.40f;
        [SerializeField] private float _feetHeight    = 0.04f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _platformLayer;

        private Rigidbody2D _rb;
        private Animator    _animator;
        private PlayerDash  _playerDash;
        private PlayerSkill _playerSkill;


        private int  _jumpCount;
        private bool _isGrounded;
        private bool _isOnPlatform;

        public bool IsGrounded => _isGrounded;

        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

        // ─── 발 감지 박스 중심 (월드 좌표) ───────────────────────────────
        private Vector2 FeetCenter  => (Vector2)transform.position + _feetOffset;
        private Vector2 FeetBoxSize => new Vector2(_feetWidth, _feetHeight);

        private void Awake()
        {
            _rb          = GetComponent<Rigidbody2D>();
            _animator    = GetComponent<Animator>();
            _playerDash  = GetComponent<PlayerDash>();
            _playerSkill = GetComponent<PlayerSkill>();
            
        }

        private void Update()
        {
            HandleMovement();
            HandleJump();
            DrawDebugGizmos();
        }

        private void FixedUpdate()
        {
            CheckGround();
        }

        // ─── 착지 감지 ────────────────────────────────────────────────────
        private void CheckGround()
        {
            // 상승 중에는 착지 판정 스킵 → 플랫폼 아래 통과 시 오감지 방지
            if (_rb.linearVelocity.y > 0.1f)
            {
                _isGrounded   = false;
                
                return;
            }

            bool wasGrounded = _isGrounded;

            bool onGround   = Physics2D.OverlapBox(FeetCenter, FeetBoxSize, 0f, _groundLayer);
            bool onPlatform = Physics2D.OverlapBox(FeetCenter, FeetBoxSize, 0f, _platformLayer);

            _isOnPlatform = onPlatform;
            _isGrounded   = onGround || onPlatform;

            // 착지 순간 점프 횟수 초기화
            if (!wasGrounded && _isGrounded)
                _jumpCount = 0;
        }

        // ─── 좌우 이동 ────────────────────────────────────────────────────
        private void HandleMovement()
        {
            // 대시(감속 포함) / 롤링 슬레쉬 중에는 이동 입력 차단
            if (_playerDash  != null && _playerDash.IsDashing) return;
            if (_playerSkill != null && _playerSkill.IsRolling)   return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float horizontal = 0f;
            if (keyboard.leftArrowKey.isPressed)  horizontal = -1f;
            if (keyboard.rightArrowKey.isPressed) horizontal =  1f;

            _rb.linearVelocity = new Vector2(horizontal * _moveSpeed, _rb.linearVelocity.y);

            if (horizontal != 0f)
                Flip(horizontal);

            _animator?.SetBool(AnimIsMoving, horizontal != 0f);
        }

        // ─── 점프 / 아래 점프 ─────────────────────────────────────────────
        private void HandleJump()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool spacePressed = keyboard.spaceKey.wasPressedThisFrame;
            bool downHeld     = keyboard.downArrowKey.isPressed;

            // 아래 점프: 플랫폼 위에 있을 때만
            if (spacePressed && downHeld && _isOnPlatform)
            {
                StartCoroutine(DropThroughPlatform());
                return;
            }

            // 일반 / 2단 점프
            if (spacePressed && _jumpCount < _maxJumpCount)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
                _jumpCount++;
                _playerDash?.OnJump(); // 점프 후 대시 잠금
            }
        }

        // ─── 플랫폼 통과 낙하 ─────────────────────────────────────────────
        // 발 아래 플랫폼 콜라이더를 isTrigger로 전환 → 즉시 통과 가능
        private IEnumerator DropThroughPlatform()
        {
            Collider2D[] cols = Physics2D.OverlapBoxAll(FeetCenter, FeetBoxSize, 0f, _platformLayer);

            foreach (var col in cols)
                col.isTrigger = true;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -4f);

            yield return new WaitForSeconds(0.4f);

            foreach (var col in cols)
                col.isTrigger = false;
        }

        // ─── 방향 전환 ────────────────────────────────────────────────────
        private void Flip(float horizontal)
        {
            Vector3 scale = transform.localScale;
            scale.x = horizontal > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // ─── 시각화 ───────────────────────────────────────────────────────
        private void DrawDebugGizmos()
        {
            Color col = _isOnPlatform ? Color.cyan
                      : _isGrounded   ? Color.green
                                      : Color.red;

            Vector2 c  = FeetCenter;
            float   hw = _feetWidth  * 0.5f;
            float   hh = _feetHeight * 0.5f;

            Debug.DrawLine(new Vector3(c.x - hw, c.y + hh), new Vector3(c.x + hw, c.y + hh), col);
            Debug.DrawLine(new Vector3(c.x + hw, c.y + hh), new Vector3(c.x + hw, c.y - hh), col);
            Debug.DrawLine(new Vector3(c.x + hw, c.y - hh), new Vector3(c.x - hw, c.y - hh), col);
            Debug.DrawLine(new Vector3(c.x - hw, c.y - hh), new Vector3(c.x - hw, c.y + hh), col);
        }

        // Editor Scene 뷰 — 항상 표시
        private void OnDrawGizmos()
        {
            Gizmos.color = Application.isPlaying
                ? (_isOnPlatform ? Color.cyan : _isGrounded ? Color.green : Color.red)
                : Color.yellow;

            Gizmos.DrawWireCube(FeetCenter, new Vector3(_feetWidth, _feetHeight, 0f));
        }
    }
}
