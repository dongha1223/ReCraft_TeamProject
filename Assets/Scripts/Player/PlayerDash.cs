using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerDash : MonoBehaviour
    {
        [SerializeField] private float _dashSpeed = 18f;
        [SerializeField] private float _dashDuration = 0.15f;
        [SerializeField] private float _dashCooldown = 1f;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        private bool _isDashing;
        private bool _canDash = true;

        private static readonly int AnimDash = Animator.StringToHash("Dash");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (_isDashing) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.zKey.wasPressedThisFrame && _canDash)
                StartCoroutine(DashCoroutine());
        }

        private IEnumerator DashCoroutine()
        {
            _canDash = false;
            _isDashing = true;

            // 현재 스프라이트 방향 기준으로 대쉬 방향 결정
            float dir = (_spriteRenderer != null && _spriteRenderer.flipX) ? -1f : 1f;

            _animator?.SetTrigger(AnimDash);
            _rb.linearVelocity = new Vector2(dir * _dashSpeed, _rb.linearVelocity.y);

            yield return new WaitForSeconds(_dashDuration);

            _isDashing = false;

            // 쿨타임 대기
            yield return new WaitForSeconds(_dashCooldown - _dashDuration);
            _canDash = true;
        }

        public bool IsDashing => _isDashing;
    }
}
