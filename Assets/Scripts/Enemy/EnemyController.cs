using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _patrolDistance = 3f;

        [Header("감지 & 공격")]
        [SerializeField] private float _detectionRange = 5f;
        [SerializeField] private float _attackRange = 0.8f;
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackCooldown = 1.2f;

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

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        private Transform        _player;
        private PlayerController _playerController;
        private Vector2 _patrolOrigin;
        private int _patrolDir = 1;
        private bool _canAttack  = true;
        private bool _isAttacking = false;

        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimAttack   = Animator.StringToHash("Attack");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _patrolOrigin = transform.position;
        }

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _player           = playerGO.transform;
                _playerController = playerGO.GetComponent<PlayerController>();
            }
        }

        // 풀에서 꺼낼 때(SetActive(true)) 상태 초기화
        private void OnEnable()
        {
            _patrolOrigin = transform.position;
            _patrolDir   = 1;
            _canAttack   = true;
            _isAttacking = false;

            // 애니메이터 파라미터 초기화
            _animator?.ResetTrigger(AnimAttack);
            _animator?.SetBool(AnimIsMoving, false);

            // 플레이어 참조 갱신 (풀 재사용 시 누락 방지)
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _player           = playerGO.transform;
                _playerController = playerGO.GetComponent<PlayerController>();
            }

            // ResetStats 호출 (EnemyStats와 연동)
            GetComponent<EnemyStats>()?.ResetStats();
        }

        private void Update()
        {
            if (_player == null) return;

            // 공격 모션 진행 중 — 수평 이동 완전 정지
            if (_isAttacking)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            // 플레이어가 다른 플랫폼 레벨에 있으면 순찰로 복귀
            if (!IsPlayerOnSamePlatform())
            {
                Patrol();
                return;
            }

            float distToPlayer = Vector2.Distance(transform.position, _player.position);

            if (distToPlayer <= _attackRange)
                HandleAttack();
            else if (distToPlayer <= _detectionRange)
                ChasePlayer();
            else
                Patrol();
        }

        /// <summary>
        /// 플레이어가 같은 플랫폼 레벨에 있는지 판정
        /// - 공중에 있는 동안은 true 유지 (점프 중 갑작스러운 추격 해제 방지)
        /// - 착지 후 Y 차이가 임계값 초과 → false (다른 플랫폼)
        /// </summary>
        private bool IsPlayerOnSamePlatform()
        {
            // PlayerController 참조 없으면 항상 같은 플랫폼으로 간주
            if (_playerController == null) return true;

            // 플레이어가 공중에 있는 동안은 추격 유지
            if (!_playerController.IsGrounded) return true;

            // 착지 후 Y 차이 비교
            return Mathf.Abs(_player.position.y - transform.position.y) <= _platformYThreshold;
        }

        private void Patrol()
        {
            float target = _patrolOrigin.x + _patrolDir * _patrolDistance;

            // 순찰 끝점 또는 발판 끝 도달 시 방향 전환
            if ((_patrolDir == 1 && transform.position.x >= target) ||
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

            // 발판 끝이면 추격 멈춤 (낙하 방지)
            if (IsLedgeAhead(dir))
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }

            Move(dir * _moveSpeed);
        }

        /// <summary>
        /// 이동 방향 앞쪽 발 아래에 지면이 없으면 true (낙하 위험)
        /// </summary>
        private bool IsLedgeAhead(float dirX)
        {
            LayerMask combined = _groundLayer | _platformLayer;
            if (combined.value == 0) return false; // 레이어 미설정 시 체크 스킵

            Vector2 origin = (Vector2)transform.position
                           + new Vector2(dirX * _ledgeCheckOffsetX, _ledgeCheckOffsetY);

            return !Physics2D.Raycast(origin, Vector2.down, _ledgeCheckDist, combined);
        }

        private void Move(float velX)
        {
            _rb.linearVelocity = new Vector2(velX, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, true);

            if (_spriteRenderer != null)
                _spriteRenderer.flipX = velX < 0f;
        }

        private void HandleAttack()
        {
            // 공격 중 이동 정지
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, false);

            if (!_canAttack) return;
            StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            _canAttack   = false;
            _isAttacking = true;
            _animator?.SetTrigger(AnimAttack);

            // 공격 판정 (모션 중간)
            yield return new WaitForSeconds(0.25f);

            if (_player != null)
            {
                float dist = Vector2.Distance(transform.position, _player.position);
                if (dist <= _attackRange)
                    _player.GetComponent<PlayerStats>()?.TakeDamage(_attackDamage);
            }

            yield return new WaitForSeconds(_attackCooldown - 0.25f);

            _isAttacking = false;
            _canAttack   = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // 낙하 감지 레이 시각화 (좌/우)
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
