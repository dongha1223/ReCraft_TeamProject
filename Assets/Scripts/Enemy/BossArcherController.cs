using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _2D_Roguelike
{
    // ── 데이터 ────────────────────────────────────────────────────────────

    [Serializable]
    public class AnimatorCommand
    {
        public enum CmdType { Trigger, BoolOn, BoolOff }
        public CmdType type;
        public string  paramName;
    }

    /// <summary>
    /// 보스 패턴 1개를 표현하는 직렬화 데이터.
    /// patternName == "Attack2" 이면 Attack2BoxRushCoroutine 전용 경로로 분기된다.
    /// 그 외 패턴은 steps[i] 실행 → waits[i] 대기를 순서대로 반복한 뒤 recoveryTime 후 종료.
    ///
    /// 예 — Block→MeleeAttack:
    ///   steps  : [Trigger:Block, BoolOn:IsMelee, BoolOff:IsMelee]
    ///   waits  : [0.5, 0.9, 0]
    ///   recovery: 0.8
    /// </summary>
    [Serializable]
    public class BossPattern
    {
        public string            patternName;
        public AnimatorCommand[] steps;
        [Tooltip("steps[i] 실행 후 대기 시간. steps보다 짧으면 나머지는 0으로 처리.")]
        public float[]           waits;
        [Tooltip("패턴 완료 후 다음 쿨타임 시작 전 회복 대기 시간")]
        public float             recoveryTime = 0.5f;
    }

    public enum BossState { Idle, Walking, ExecutingPattern, Recovery, Dead }

    // ── 컨트롤러 ──────────────────────────────────────────────────────────

    public class BossArcherController : MonoBehaviour, IStatusLockable
    {
        // TODO: 카메라 전환·문 잠금·보상 처리 등 씬 제어는
        //       BossRoomManager 같은 전용 스테이지 매니저가 이 이벤트를 구독하여 담당.
        public static event Action OnBossEngaged;
        public static event Action OnBossDead;

        [Header("참조")]
        [SerializeField] private Animator _animator;

        [Header("이동")]
        [SerializeField] private float _moveSpeed = 2f;

        [Header("이동 & 순찰")]
        [Tooltip("이 거리 초과 시 추격 시작 (chaseStopRange보다 크게 설정)")]
        [SerializeField] private float _chaseStartRange = 7f;
        [Tooltip("이 거리 이하로 좁혀지면 추격 해제 → 순찰 복귀")]
        [SerializeField] private float _chaseStopRange  = 5f;
        [Tooltip("초기 위치 기준 좌우 최대 순찰 이동 거리")]
        [SerializeField] private float _patrolDistance  = 4f;
        [Tooltip("순찰 방향 전환 주기 최솟값 (초)")]
        [SerializeField] private float _patrolChangeMin = 1f;
        [Tooltip("순찰 방향 전환 주기 최댓값 (초)")]
        [SerializeField] private float _patrolChangeMax = 3f;

        [Header("패턴 쿨타임")]
        [SerializeField] private float _cooldownMin = 2f;
        [SerializeField] private float _cooldownMax = 4f;

        [Header("패턴 목록 (Inspector에서 직접 설정)")]
        [SerializeField] private BossPattern[] _patterns;

        [Header("Attack2 — 박스 일직선 발사")]
        [Tooltip("박스형 AreaSkillSpec SO를 연결한다")]
        [SerializeField] private AreaSkillSpec _attack2Spec;
        [Tooltip("박스 발사 개수")]
        [SerializeField] private int           _attack2BoxCount    = 4;
        [Tooltip("보스 정면 방향 기준 박스 간 X 간격 (m)")]
        [SerializeField] private float         _attack2BoxStep     = 1.5f;
        [Tooltip("박스 간 발동 딜레이 (초)")]
        [SerializeField] private float         _attack2BoxInterval = 0.3f;
        [Tooltip("Attack2 애니메이션 후 박스 발사 시작까지 선딜 (초)")]
        [SerializeField] private float         _attack2PreDelay    = 0.5f;
        [Tooltip("각 박스 위치에 스폰할 이펙트 프리팹 (없으면 생략)")]
        [SerializeField] private GameObject    _attack2EffectPrefab;
        [Tooltip("이펙트 스폰 Y 오프셋 (음수면 아래로)")]
        [SerializeField] private float         _attack2EffectOffsetY = 0f;
        [Tooltip("이펙트 스폰 후 실제 데미지 판정까지 선딜 (초)")]
        [SerializeField] private float         _attack2DamageDelay   = 0.5f;

        private Rigidbody2D       _rb;
        private Transform         _player;
        private AreaSkillExecutor _areaExecutor;

        private BossState _state          = BossState.Idle;
        private int       _lastPatternIdx = -1;
        private Coroutine _patternLoopHandle;

        // 순찰/추격 상태
        private Vector2 _patrolOrigin;
        private int     _patrolDir            = 1;
        private float   _patrolTimer          = 0f;
        private float   _patrolChangeDuration = 1f;
        private bool    _isChasing            = false;

        // ── IStatusLockable ───────────────────────────────────────────────
        private int  _actionLockCount;
        private int  _frozenCount;
        private bool IsActionLocked => _actionLockCount > 0;
        private bool IsFrozen       => _frozenCount      > 0;

        // ── Animator 파라미터 해시 ────────────────────────────────────────
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsMelee  = Animator.StringToHash("IsMelee");
        private static readonly int AnimIsDash   = Animator.StringToHash("IsDash");
        private static readonly int AnimDie      = Animator.StringToHash("Die");
        private static readonly int AnimAttack2  = Animator.StringToHash("Attack2");

        // ── 생명주기 ─────────────────────────────────────────────────────

        private void Awake()
        {
            _rb           = GetComponent<Rigidbody2D>();
            _areaExecutor = GetComponent<AreaSkillExecutor>();
            _patrolOrigin = transform.position;
            _patrolChangeDuration = Random.Range(_patrolChangeMin, _patrolChangeMax);
        }

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;

            OnBossEngaged?.Invoke();
            _patternLoopHandle = StartCoroutine(PatternLoop());
        }

        private void Update()
        {
            if (IsActionLocked)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }

            switch (_state)
            {
                case BossState.Idle:
                case BossState.Walking:
                    HandleMovement();
                    break;

                default:
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _animator?.SetBool(AnimIsMoving, false);
                    break;
            }
        }

        // ── 이동 ─────────────────────────────────────────────────────────

        private void HandleMovement()
        {
            if (_player == null)
            {
                Patrol();
                return;
            }

            float dist = Vector2.Distance(transform.position, _player.position);

            // 히스테리시스: 시작/해제 거리를 분리해 경계선 진동 방지
            if (!_isChasing && dist > _chaseStartRange)
                _isChasing = true;
            else if (_isChasing && dist <= _chaseStopRange)
                _isChasing = false;

            if (_isChasing)
                ChasePlayer();
            else
                Patrol();
        }

        private void ChasePlayer()
        {
            float dir = _player.position.x > transform.position.x ? 1f : -1f;
            _rb.linearVelocity = new Vector2(dir * _moveSpeed, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, true);
            Flip(dir);
        }

        private void Patrol()
        {
            _patrolTimer += Time.deltaTime;

            bool outOfBounds = (_patrolDir ==  1 && transform.position.x >= _patrolOrigin.x + _patrolDistance) ||
                               (_patrolDir == -1 && transform.position.x <= _patrolOrigin.x - _patrolDistance);

            if (outOfBounds || _patrolTimer >= _patrolChangeDuration)
            {
                // 범위 이탈 시 반드시 반전, 타이머 만료 시 랜덤
                _patrolDir            = outOfBounds ? -_patrolDir : (Random.value > 0.5f ? 1 : -1);
                _patrolTimer          = 0f;
                _patrolChangeDuration = Random.Range(_patrolChangeMin, _patrolChangeMax);
            }

            _rb.linearVelocity = new Vector2(_patrolDir * _moveSpeed, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, true);
            Flip(_patrolDir);
        }

        private void Flip(float dirX)
        {
            Vector3 scale = transform.localScale;
            scale.x = dirX > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // ── 패턴 루프 ────────────────────────────────────────────────────

        private IEnumerator PatternLoop()
        {
            while (_state != BossState.Dead)
            {
                _state = BossState.Idle;

                yield return StartCoroutine(PauseableWait(Random.Range(_cooldownMin, _cooldownMax)));

                if (_state == BossState.Dead || _patterns == null || _patterns.Length == 0)
                    yield break;

                int idx = SelectPattern();
                _lastPatternIdx = idx;

                _state = BossState.ExecutingPattern;
                yield return StartCoroutine(ExecutePattern(_patterns[idx]));

                _state = BossState.Recovery;
                yield return StartCoroutine(PauseableWait(_patterns[idx].recoveryTime));
            }
        }

        private int SelectPattern()
        {
            if (_patterns.Length == 1) return 0;

            int idx;
            int attempts = 0;
            do
            {
                idx = Random.Range(0, _patterns.Length);
                attempts++;
            }
            while (idx == _lastPatternIdx && attempts < 10);

            return idx;
        }

        private IEnumerator ExecutePattern(BossPattern pattern)
        {
            // Attack2 전용 경로
            if (pattern.patternName == "Attack2")
            {
                yield return StartCoroutine(Attack2BoxRushCoroutine());
                yield break;
            }

            if (pattern.steps == null) yield break;

            for (int i = 0; i < pattern.steps.Length; i++)
            {
                ApplyAnimatorCommand(pattern.steps[i]);

                float wait = (pattern.waits != null && i < pattern.waits.Length)
                           ? pattern.waits[i]
                           : 0f;

                if (wait > 0f)
                    yield return StartCoroutine(PauseableWait(wait));
            }
        }

        // ── Attack2: 박스 일직선 순차 발사 ──────────────────────────────

        private IEnumerator Attack2BoxRushCoroutine()
        {
            // ① 발사 전 플레이어 방향으로 전환
            if (_player != null)
                Flip(_player.position.x > transform.position.x ? 1f : -1f);

            // ② Attack2 애니메이션 트리거 → 선딜 대기
            _animator?.SetTrigger(AnimAttack2);
            yield return StartCoroutine(PauseableWait(_attack2PreDelay));

            // ③ 발사 중 Idle 유지
            _animator?.SetBool(AnimIsMoving, false);

            float   dir     = transform.localScale.x >= 0f ? 1f : -1f;
            Vector2 forward = new Vector2(dir, 0f);

            // ④ 박스 순차 발동: 이펙트 → 딜레이 → 데미지 → 다음 박스 간격
            for (int i = 0; i < _attack2BoxCount; i++)
            {
                Vector2 origin = (Vector2)transform.position
                               + new Vector2(dir * _attack2BoxStep * (i + 1), 0f);

                // 이펙트 먼저 (예고 시각화)
                if (_attack2EffectPrefab != null)
                {
                    Vector2 effectPos = origin + new Vector2(0f, _attack2EffectOffsetY);
                    Instantiate(_attack2EffectPrefab, effectPos, Quaternion.identity);
                }

                // 데미지 선딜
                if (_attack2DamageDelay > 0f)
                    yield return StartCoroutine(PauseableWait(_attack2DamageDelay));

                _areaExecutor?.Execute(_attack2Spec, origin, forward);

                // 다음 박스까지 간격
                if (i < _attack2BoxCount - 1)
                    yield return StartCoroutine(PauseableWait(_attack2BoxInterval));
            }
        }

        private void ApplyAnimatorCommand(AnimatorCommand cmd)
        {
            if (_animator == null || string.IsNullOrEmpty(cmd.paramName)) return;

            switch (cmd.type)
            {
                case AnimatorCommand.CmdType.Trigger:
                    _animator.SetTrigger(cmd.paramName);
                    break;
                case AnimatorCommand.CmdType.BoolOn:
                    _animator.SetBool(cmd.paramName, true);
                    break;
                case AnimatorCommand.CmdType.BoolOff:
                    _animator.SetBool(cmd.paramName, false);
                    break;
            }
        }

        // ── 사망 ─────────────────────────────────────────────────────────

        /// <summary>EnemyStats 등 체력 시스템에서 사망 확정 시 호출.</summary>
        public void OnDead()
        {
            if (_state == BossState.Dead) return;
            _state = BossState.Dead;

            if (_patternLoopHandle != null)
            {
                StopCoroutine(_patternLoopHandle);
                _patternLoopHandle = null;
            }

            _rb.linearVelocity = Vector2.zero;
            _animator?.SetBool(AnimIsMelee,  false);
            _animator?.SetBool(AnimIsDash,   false);
            _animator?.SetBool(AnimIsMoving, false);
            _animator?.SetTrigger(AnimDie);

            OnBossDead?.Invoke();
        }

        // ── IStatusLockable ───────────────────────────────────────────────

        public void ApplyActionLock(bool cancelOngoing)
        {
            _actionLockCount++;

            if (cancelOngoing)
            {
                if (_patternLoopHandle != null)
                {
                    StopCoroutine(_patternLoopHandle);
                    _patternLoopHandle = null;
                }
                _animator?.SetBool(AnimIsMelee,  false);
                _animator?.SetBool(AnimIsDash,   false);
                _animator?.SetBool(AnimIsMoving, false);
                _state = BossState.Idle;
            }
            else
            {
                _frozenCount++;
            }
        }

        public void RemoveActionLock(bool wasCancelled)
        {
            _actionLockCount = Mathf.Max(0, _actionLockCount - 1);
            if (!wasCancelled)
                _frozenCount = Mathf.Max(0, _frozenCount - 1);

            if (!IsActionLocked && _patternLoopHandle == null && _state != BossState.Dead)
                _patternLoopHandle = StartCoroutine(PatternLoop());
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

        private IEnumerator PauseableWait(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsFrozen) elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // ── 에디터 Gizmo ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 추격 시작 거리 (노랑), 해제 거리 (초록)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _chaseStartRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _chaseStopRange);

            // 순찰 범위
            Gizmos.color = Color.cyan;
            Vector3 center = Application.isPlaying ? (Vector3)_patrolOrigin : transform.position;
            Gizmos.DrawLine(center + Vector3.left  * _patrolDistance, center + Vector3.right * _patrolDistance);
            Gizmos.DrawWireSphere(center + Vector3.left  * _patrolDistance, 0.1f);
            Gizmos.DrawWireSphere(center + Vector3.right * _patrolDistance, 0.1f);

            // Attack2 박스 범위
            if (_attack2Spec == null || _attack2Spec.ShapeType != AreaShapeType.Box) return;

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            float dir = transform.localScale.x >= 0f ? 1f : -1f;
            for (int i = 0; i < _attack2BoxCount; i++)
            {
                Vector3 origin = transform.position + new Vector3(dir * _attack2BoxStep * (i + 1), 0f, 0f);
                Gizmos.DrawWireCube(origin, _attack2Spec.BoxSize);
            }
        }
#endif
    }
}
