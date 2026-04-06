using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 모든 적 AI의 공통 이동/순찰/플랫폼 로직을 담당하는 추상 기반 클래스.
    /// 서브클래스는 AttackCoroutine()만 구현하면 됩니다.
    /// </summary>
    public abstract class EnemyBrainBase : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] protected float _moveSpeed      = 2f;
        [SerializeField] protected float _patrolDistance = 3f;

        [Header("감지 & 공격")]
        [SerializeField] protected float _detectionRange = 5f;
        [SerializeField] protected float _attackRange    = 0.8f;
        [SerializeField] protected float _attackCooldown = 1.2f;

        [Header("플랫폼 인식")]
        [Tooltip("이 값 이상 Y 차이가 나면 다른 플랫폼으로 간주하고 순찰로 복귀")]
        [SerializeField] private float _platformYThreshold = 1.5f;

        [Header("발판 이탈 방지")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _platformLayer;
        [Tooltip("발 앞쪽 수평 오프셋 (이 거리 앞에서 지면 체크)")]
        [SerializeField] private float _ledgeCheckOffsetX = 0.4f;
        [Tooltip("체크 시작점의 수직 오프셋 (발 높이 기준)")]
        [SerializeField] private float _ledgeCheckOffsetY = -0.1f;
        [Tooltip("아래 방향 레이캐스트 거리")]
        [SerializeField] private float _ledgeCheckDist    = 0.8f;

        protected Rigidbody2D       _rb;
        protected Animator          _animator;
        protected KnockbackReceiver _knockback;
        protected Transform         _player;
        protected PlayerController  _playerController;

        protected Vector2 _patrolOrigin;
        protected int     _patrolDir   = 1;
        protected bool    _canAttack   = true;
        protected bool    _isAttacking = false;

        protected static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

        protected virtual void Awake()
        {
            _rb           = GetComponent<Rigidbody2D>();
            _animator     = GetComponent<Animator>();
            _knockback    = GetComponent<KnockbackReceiver>();
            _patrolOrigin = transform.position;
        }

        protected virtual void Start()
        {
            FindPlayer();
        }

        protected virtual void OnEnable()
        {
            _patrolOrigin = transform.position;
            _patrolDir    = 1;
            _canAttack    = true;
            _isAttacking  = false;
            _knockback?.ResetKnockback();

            _animator?.SetBool(AnimIsMoving, false);

            FindPlayer();
            GetComponent<EnemyStats>()?.ResetStats();
        }

        private void FindPlayer()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO == null) return;
            _player           = playerGO.transform;
            _playerController = playerGO.GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_player == null) return;

            // 스태거 중 — AI 중단, 외부 힘만 반영
            if (_knockback != null && _knockback.IsKnockedBack)
            {
                _rb.linearVelocity = new Vector2(_knockback.ExternalVelocity.x, _rb.linearVelocity.y);
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }

            // 공격 모션 진행 중 — 수평 이동 정지
            if (_isAttacking)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            HandleAI();
        }

        /// <summary>
        /// AI 판단 루프. 서브클래스에서 override 가능 (예: 정지형 적).
        /// 기본 동작: 순찰 → 감지 → 추격 → 공격
        /// </summary>
        protected virtual void HandleAI()
        {
            if (!IsPlayerOnSamePlatform())
            {
                Patrol();
                return;
            }

            float dist = Vector2.Distance(transform.position, _player.position);

            if (dist <= _attackRange)
                HandleAttack();
            else if (dist <= _detectionRange)
                ChasePlayer();
            else
                Patrol();
        }

        /// <summary>
        /// 공격 진입 처리. 이동 정지 후 AttackCoroutine 시작.
        /// 서브클래스에서 override 시 base.HandleAttack() 호출 권장.
        /// </summary>
        protected virtual void HandleAttack()
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, false);

            if (!_canAttack) return;
            StartCoroutine(AttackCoroutine());
        }

        /// <summary>실제 공격 패턴 구현. 서브클래스에서 반드시 구현.</summary>
        protected abstract IEnumerator AttackCoroutine();

        // ── 이동 ──────────────────────────────────────────────────────────

        private void Patrol()
        {
            float target = _patrolOrigin.x + _patrolDir * _patrolDistance;

            if ((_patrolDir == 1  && transform.position.x >= target) ||
                (_patrolDir == -1 && transform.position.x <= target) ||
                IsLedgeAhead(_patrolDir))
            {
                _patrolDir *= -1;
            }

            Move(_patrolDir * _moveSpeed);
        }

        private void ChasePlayer()
        {
            float dir = _player.position.x > transform.position.x ? 1f : -1f;

            if (IsLedgeAhead(dir))
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }

            Move(dir * _moveSpeed);
        }

        protected void Move(float velX)
        {
            float externalX = _knockback != null ? _knockback.ExternalVelocity.x : 0f;
            _rb.linearVelocity = new Vector2(velX + externalX, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, true);
            Flip(velX);
        }

        protected void Flip(float dirX)
        {
            Vector3 scale = transform.localScale;
            scale.x = dirX > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // ── 판정 헬퍼 ────────────────────────────────────────────────────

        /// <summary>
        /// 플레이어가 같은 플랫폼 레벨에 있는지 판정.
        /// 공중에 있는 동안은 true 유지 (점프 중 갑작스러운 추격 해제 방지).
        /// </summary>
        private bool IsPlayerOnSamePlatform()
        {
            if (_playerController == null) return true;
            if (!_playerController.IsGrounded) return true;
            return Mathf.Abs(_player.position.y - transform.position.y) <= _platformYThreshold;
        }

        /// <summary>이동 방향 앞쪽 발 아래에 지면이 없으면 true (낙하 위험).</summary>
        private bool IsLedgeAhead(float dirX)
        {
            LayerMask combined = _groundLayer | _platformLayer;
            if (combined.value == 0) return false;

            Vector2 origin = (Vector2)transform.position
                           + new Vector2(dirX * _ledgeCheckOffsetX, _ledgeCheckOffsetY);

            return !Physics2D.Raycast(origin, Vector2.down, _ledgeCheckDist, combined);
        }

        // ── 디버그 ────────────────────────────────────────────────────────

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            Gizmos.color = Color.cyan;
            foreach (float dir in new[] { 1f, -1f })
            {
                Vector2 origin = (Vector2)transform.position
                               + new Vector2(dir * _ledgeCheckOffsetX, _ledgeCheckOffsetY);
                Gizmos.DrawLine(origin, origin + Vector2.down * _ledgeCheckDist);
                Gizmos.DrawWireSphere(origin, 0.05f);
            }
        }
    }
}
