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

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        private Transform _player;
        private Vector2 _patrolOrigin;
        private int _patrolDir = 1;
        private bool _canAttack = true;

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
            if (playerGO != null) _player = playerGO.transform;
        }

        // 풀에서 꺼낼 때(SetActive(true)) 상태 초기화
        private void OnEnable()
        {
            _patrolOrigin = transform.position;
            _patrolDir = 1;
            _canAttack = true;

            // 애니메이터 파라미터 초기화
            _animator?.ResetTrigger(AnimAttack);
            _animator?.SetBool(AnimIsMoving, false);

            // 플레이어 참조 갱신 (풀 재사용 시 누락 방지)
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;

            // ResetStats 호출 (EnemyStats와 연동)
            GetComponent<EnemyStats>()?.ResetStats();
        }

        private void Update()
        {
            if (_player == null) return;

            float distToPlayer = Vector2.Distance(transform.position, _player.position);

            if (distToPlayer <= _attackRange)
                HandleAttack();
            else if (distToPlayer <= _detectionRange)
                ChasePlayer();
            else
                Patrol();
        }

        private void Patrol()
        {
            float target = _patrolOrigin.x + _patrolDir * _patrolDistance;
            float moveX = _patrolDir * _moveSpeed;

            // 순찰 끝점 도달 시 방향 전환
            if ((_patrolDir == 1 && transform.position.x >= target) ||
                (_patrolDir == -1 && transform.position.x <= target))
            {
                _patrolDir *= -1;
            }

            Move(moveX);
        }

        private void ChasePlayer()
        {
            float dir = _player.position.x > transform.position.x ? 1f : -1f;
            Move(dir * _moveSpeed);
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
            _canAttack = false;
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
            _canAttack = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }
    }
}
