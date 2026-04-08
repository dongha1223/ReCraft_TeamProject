using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 넉백 물리 처리 컴포넌트.
    /// - externalVelocity.x 채널에 힘을 주입하고 매 프레임 감쇠시킨다.
    /// - Controller는 ExternalVelocity.x를 자신의 이동 velocity에 더해서 적용한다.
    /// - IsKnockedBack은 외부 힘이 staggerThreshold 이상일 때 true → 적 AI 중단에 사용.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour, IKnockbackReceiver
    {
        [Tooltip("넉백 수평 감쇠 속도 (클수록 빨리 멈춤)")]
        [SerializeField] private float _decayRate        = 12f;

        [Tooltip("넉백 발생 시 가해지는 수직 impulse")]
        [SerializeField] private float _verticalForce    = 3f;

        [Tooltip("이 값 이상이면 IsKnockedBack = true (적 AI 스태거 기준)")]
        [SerializeField] private float _staggerThreshold = 2f;

        [Tooltip("넉백 저항값. 들어오는 force에서 차감")]
        [SerializeField] private float _resistance       = 0f;

        private Rigidbody2D _rb;
        private Vector2     _externalVelocity;

        public bool    IsKnockedBack    => Mathf.Abs(_externalVelocity.x) >= _staggerThreshold;
        public Vector2 ExternalVelocity => _externalVelocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // X만 감쇠 (Y는 중력이 처리)
            _externalVelocity.x = Mathf.Lerp(_externalVelocity.x, 0f, _decayRate * Time.deltaTime);
        }

        public void ApplyKnockback(Vector2 sourcePosition, float force)
        {
            float finalForce = Mathf.Max(0f, force - _resistance);
            if (finalForce <= 0f) return;

            Vector2 dir = ((Vector2)transform.position - sourcePosition).normalized;

            // X: 감쇠 채널에 누적 (중첩 넉백 허용)
            _externalVelocity.x += dir.x * finalForce;

            // Y: 일회성 impulse — 이후 중력이 자연스럽게 감쇠
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _verticalForce);
        }

        /// <summary>풀 반환 등 강제 초기화 시 호출</summary>
        public void ResetKnockback()
        {
            _externalVelocity = Vector2.zero;
        }
    }
}
