using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyRangedController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float _moveSpeed      = 2f;
        [SerializeField] private float _patrolDistance = 3f;

        [Header("감지 & 공격")]
        [SerializeField] private float _detectionRange = 7f;
        [SerializeField] private float _attackRange    = 5f;
        [SerializeField] private float _attackDamage   = 8f;
        [SerializeField] private float _windupDuration = 1f;
        [SerializeField] private float _attackCooldown = 2f;

        [Header("넉백")]
        [SerializeField] private float _knockbackForce = 3f;

        [Header("투사체")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform  _spawnPoint;
        [SerializeField] private GameObject _windupIndicator;

        [Header("플랫폼 인식")]
        [Tooltip("이 값 이상 Y 차이가 나면 다른 플랫폼으로 간주하고 순찰로 복귀")]
        [SerializeField] private float _platformYThreshold = 1.5f;

        [Header("발판 이탈 방지")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _platformLayer;
        [Tooltip("발 앞쪽 수평 오프셋")]
        [SerializeField] private float _ledgeCheckOffsetX = 0.4f;
        [Tooltip("체크 시작점의 수직 오프셋")]
        [SerializeField] private float _ledgeCheckOffsetY = -0.1f;
        [Tooltip("아래 방향 레이캐스트 거리")]
        [SerializeField] private float _ledgeCheckDist    = 0.8f;

        private Rigidbody2D       _rb;
        private Animator          _animator;
        private KnockbackReceiver _knockback;
        private Transform         _player;
        private PlayerController  _playerController;

        private Vector2 _patrolOrigin;
        private int     _patrolDir   = 1;
        private bool    _canAttack   = true;
        private bool    _isAttacking = false;

        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimWindup   = Animator.StringToHash("Windup");

        private void Awake()
        {
            _rb           = GetComponent<Rigidbody2D>();
            _animator     = GetComponent<Animator>();
            _knockback    = GetComponent<KnockbackReceiver>();
            _patrolOrigin = transform.position;

            if (_windupIndicator != null)
                _windupIndicator.SetActive(false);
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
            _knockback?.ResetKnockback();

            _animator?.ResetTrigger(AnimWindup);
            _animator?.SetBool(AnimIsMoving, false);

            if (_windupIndicator != null)
                _windupIndicator.SetActive(false);

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _player           = playerGO.transform;
                _playerController = playerGO.GetComponent<PlayerController>();
            }

            GetComponent<EnemyStats>()?.ResetStats();
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

            // 공격(전조) 진행 중 — 이동 정지
            if (_isAttacking)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

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
        /// 플레이어가 같은 플랫폼 레벨에 있는지 판정
        /// </summary>
        private bool IsPlayerOnSamePlatform()
        {
            if (_playerController == null) return true;
            if (!_playerController.IsGrounded) return true;
            return Mathf.Abs(_player.position.y - transform.position.y) <= _platformYThreshold;
        }

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

        /// <summary>
        /// 이동 방향 앞쪽 발 아래에 지면이 없으면 true (낙하 위험)
        /// </summary>
        private bool IsLedgeAhead(float dirX)
        {
            LayerMask combined = _groundLayer | _platformLayer;
            if (combined.value == 0) return false;

            Vector2 origin = (Vector2)transform.position
                           + new Vector2(dirX * _ledgeCheckOffsetX, _ledgeCheckOffsetY);

            return !Physics2D.Raycast(origin, Vector2.down, _ledgeCheckDist, combined);
        }

        private void Move(float velX)
        {
            float externalX = _knockback != null ? _knockback.ExternalVelocity.x : 0f;
            _rb.linearVelocity = new Vector2(velX + externalX, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, true);
            Flip(velX);
        }

        private void Flip(float dirX)
        {
            Vector3 scale = transform.localScale;
            scale.x = dirX > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        private void HandleAttack()
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, false);

            // 플레이어 방향으로 전환
            if (_player != null)
                Flip(_player.position.x > transform.position.x ? 1f : -1f);

            if (!_canAttack) return;
            StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            _canAttack   = false;
            _isAttacking = true;

            // ── 전조 연출 시작 ───────────────────────────
            _animator?.SetTrigger(AnimWindup);
            if (_windupIndicator != null)
                _windupIndicator.SetActive(true);

            yield return new WaitForSeconds(_windupDuration);

            // ── 전조 종료 → 투사체 발사 ─────────────────
            if (_windupIndicator != null)
                _windupIndicator.SetActive(false);

            if (_projectilePrefab != null && _player != null)
            {
                var go = Instantiate(_projectilePrefab, _spawnPoint.position, Quaternion.identity);
                go.GetComponent<ProjectileBase>()?.Setup(_player, new HitInfo
                {
                    Damage         = _attackDamage,
                    KnockbackForce = _knockbackForce
                    // SourcePosition은 투사체가 충돌 시점에 자신 위치로 설정
                });
            }

            // 쿨타임 잔여 대기 (windupDuration이 cooldown보다 길어지는 케이스 방지)
            yield return new WaitForSeconds(Mathf.Max(0f, _attackCooldown - _windupDuration));

            _isAttacking = false;
            _canAttack   = true;
        }

        private void OnDrawGizmosSelected()
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
