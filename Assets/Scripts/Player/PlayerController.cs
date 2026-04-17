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

        private Rigidbody2D       _rb;
        private Animator          _animator;
        private PlayerDash            _playerDash;
        private PlayerAttack          _playerAttack;
        private FormSkillController   _formSkillController;
        private KnockbackReceiver     _knockback;


        private int  _jumpCount;
        private bool _isGrounded;
        private bool _isOnPlatform;

        public bool IsGrounded => _isGrounded;

        private static readonly int AnimIsMoving  = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");

        // ─── 발 감지 박스 중심 (월드 좌표) ───────────────────────────────
        private Vector2 FeetCenter  => (Vector2)transform.position + _feetOffset;
        private Vector2 _feetBoxSize; // Awake에서 캐싱

        private void Awake()
        {
            _rb          = GetComponent<Rigidbody2D>();
            _animator    = GetComponent<Animator>();
            _playerDash          = GetComponent<PlayerDash>();
            _playerAttack        = GetComponent<PlayerAttack>();
            _formSkillController = GetComponent<FormSkillController>();
            _knockback           = GetComponent<KnockbackReceiver>();
            _feetBoxSize = new Vector2(_feetWidth, _feetHeight);
        }

        private void Update()
        {
            HandleMovement();
            HandleJump();
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

            bool onGround   = Physics2D.OverlapBox(FeetCenter, _feetBoxSize, 0f, _groundLayer);
            bool onPlatform = Physics2D.OverlapBox(FeetCenter, _feetBoxSize, 0f, _platformLayer);

            _isOnPlatform = onPlatform;
            _isGrounded   = onGround || onPlatform;

            // 착지 순간 점프 횟수 초기화
            if (!wasGrounded && _isGrounded)
                _jumpCount = 0;

            _animator?.SetBool(AnimIsJumping, !_isGrounded);
        }

        // ─── 좌우 이동 ────────────────────────────────────────────────────
        private void HandleMovement()
        {
            // UI 차단 중(대화·포즈·인벤) / 대시(감속 포함) / 롤링 슬레쉬 중에는 이동 입력 차단
            if (UIState.IsBlockingInput)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }
            if (_playerDash          != null && _playerDash.IsDashing)             return;
            if (_formSkillController != null && _formSkillController.IsRolling)   return;
            if (_playerAttack        != null && _playerAttack.IsAttacking)
            {
                // 공격 중 이동 입력 차단 — 임펄스로 부여된 속도는 그대로 유지
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }

            float horizontal = 0f;
            if (KeyBindingService.IsPressed(KeyBindingService.Action.MoveLeft))  horizontal = -1f;
            if (KeyBindingService.IsPressed(KeyBindingService.Action.MoveRight)) horizontal =  1f;

            // 입력 이동 + 외부 힘(넉백 등) 합산
            float externalX = _knockback != null ? _knockback.ExternalVelocity.x : 0f;
            _rb.linearVelocity = new Vector2(horizontal * _moveSpeed + externalX, _rb.linearVelocity.y);

            if (horizontal != 0f)
                Flip(horizontal);

            _animator?.SetBool(AnimIsMoving, horizontal != 0f);
        }

        // ─── 점프 / 아래 점프 ─────────────────────────────────────────────
        private void HandleJump()
        {
            if (UIState.IsBlockingInput) return;
            bool spacePressed = KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Jump);
            bool downHeld     = KeyBindingService.IsPressed(KeyBindingService.Action.MoveDown);

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
            Collider2D[] cols = Physics2D.OverlapBoxAll(FeetCenter, _feetBoxSize, 0f, _platformLayer);

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

        // ─── 시각화 (Scene 뷰 — 항상 표시) ──────────────────────────────
        private void OnDrawGizmos()
        {
            Gizmos.color = Application.isPlaying
                ? (_isOnPlatform ? Color.cyan : _isGrounded ? Color.green : Color.red)
                : Color.yellow;

            Gizmos.DrawWireCube(FeetCenter, new Vector3(_feetWidth, _feetHeight, 0f));
        }
    }
}
