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
    /// steps[i] 실행 → waits[i]만큼 대기를 순서대로 반복한 뒤 recoveryTime 후 다음 패턴으로 이어진다.
    /// Inspector에서 직접 설정한다.
    ///
    /// 예 — Block→MeleeAttack:
    ///   steps  : [Trigger:Block, BoolOn:IsMelee, BoolOff:IsMelee]
    ///   waits  : [0.5, 0.9, 0]
    ///   recovery: 0.8
    ///
    /// 예 — Dash→JumpAttack:
    ///   steps  : [BoolOn:IsDash, Trigger:JumpAttack, BoolOff:IsDash]
    ///   waits  : [0.6, 1.1, 0]
    ///   recovery: 0.9
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
        // ── 보스 전투 이벤트 ──────────────────────────────────────────────
        // TODO: 카메라 전환·문 잠금·보상 처리 등 씬 제어는
        //       BossRoomManager 같은 전용 스테이지 매니저가 이 이벤트를 구독하여 담당.
        //       보스 컨트롤러는 이벤트 발행만 하고 씬에 직접 개입하지 않는다.
        public static event Action OnBossEngaged;
        public static event Action OnBossDead;

        [Header("참조")]
        [SerializeField] private Animator _animator;

        [Header("이동")]
        [SerializeField] private float _moveSpeed = 2f;

        [Header("패턴 쿨타임")]
        [SerializeField] private float _cooldownMin = 2f;
        [SerializeField] private float _cooldownMax = 4f;

        [Header("패턴 목록 (Inspector에서 직접 설정)")]
        [SerializeField] private BossPattern[] _patterns;

        private Rigidbody2D _rb;
        private Transform   _player;

        private BossState _state          = BossState.Idle;
        private int       _lastPatternIdx = -1;
        private Coroutine _patternLoopHandle;

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

        // ── 생명주기 ─────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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
            if (_player == null || IsActionLocked)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _animator?.SetBool(AnimIsMoving, false);
                return;
            }

            switch (_state)
            {
                case BossState.Idle:
                case BossState.Walking:
                    MoveTowardPlayer();
                    FacePlayer();
                    break;

                default:
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _animator?.SetBool(AnimIsMoving, false);
                    break;
            }
        }

        // ── 이동 ─────────────────────────────────────────────────────────

        private void MoveTowardPlayer()
        {
            float dir = _player.position.x > transform.position.x ? 1f : -1f;
            _rb.linearVelocity = new Vector2(dir * _moveSpeed, _rb.linearVelocity.y);
            _animator?.SetBool(AnimIsMoving, true);
        }

        private void FacePlayer()
        {
            float diff = _player.position.x - transform.position.x;
            if (Mathf.Abs(diff) > 0.05f) Flip(diff);
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

                // 패턴 실행 전 플레이어 방향 확정
                if (_player != null)
                    Flip(_player.position.x > transform.position.x ? 1f : -1f);

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

            // TODO: 공격 판정 삽입 위치
            // - Attack1/2/3 : 화살 발사 (ProjectileBase.Setup 호출)
            // - Block→MeleeAttack : 근접 히트박스 활성화
            // - Dash→JumpAttack   : 대시 이동 + 착지 충격파
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

        /// <summary>
        /// EnemyStats 등 체력 시스템에서 사망 확정 시 호출.
        /// </summary>
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
    }
}
