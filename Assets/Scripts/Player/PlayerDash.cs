using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어 대시
    /// 수정 사항:
    ///   - _dashCooldown < _dashDuration 일 때 WaitForSeconds 음수 방지
    ///   - 쿨다운은 대시 시작 시점부터 측정 (더 자연스러운 UX)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerDash : MonoBehaviour
    {
        [SerializeField] private float _dashSpeed    = 18f;
        [SerializeField] private float _dashDuration = 0.15f;
        [SerializeField] private float _dashCooldown = 1f;

        private Rigidbody2D    _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator       _animator;

        private bool _isDashing;
        private bool _canDash = true;

        private static readonly int AnimDash = Animator.StringToHash("Dash");

        public bool IsDashing => _isDashing;

        private void Awake()
        {
            _rb             = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator       = GetComponent<Animator>();
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
            _canDash   = false;
            _isDashing = true;

            float dir = (_spriteRenderer != null && _spriteRenderer.flipX) ? -1f : 1f;
            _animator?.SetTrigger(AnimDash);
            _rb.linearVelocity = new Vector2(dir * _dashSpeed, _rb.linearVelocity.y);

            // 대시 지속 시간 대기
            yield return new WaitForSeconds(Mathf.Max(0f, _dashDuration));

            _isDashing = false;

            // 쿨다운: 대시 시간 이후 남은 시간만큼 추가 대기 (음수 방지)
            float remainCooldown = Mathf.Max(0f, _dashCooldown - _dashDuration);
            yield return new WaitForSeconds(remainCooldown);

            _canDash = true;
        }
    }
}
