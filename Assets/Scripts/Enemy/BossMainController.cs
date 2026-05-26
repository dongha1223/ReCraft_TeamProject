using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _2D_Roguelike
{
    public enum BossMainPhase { Phase1, Phase2 }
    public enum BossMainState { Idle, ExecutingPattern, Recovery, Dead }

    public class BossMainController : MonoBehaviour, IStatusLockable
    {
        // TODO: 카메라 전환·문 잠금·보스 HP바 등 씬 연출은
        //       BossRoomManager가 이 이벤트를 구독하여 담당.
        public static event Action OnBossEngaged;
        public static event Action OnBossDead;

        [Header("파츠 목록")]
        [Tooltip("Boss_Body, Boss_Head, Boss_left, Boss_right 순서로 연결")]
        [SerializeField] private BossPartHP[] _parts;

        [Tooltip("이 수 이상의 파츠가 파괴되면 Phase2로 전환 (기본 2)")]
        [SerializeField] private int _phase2DestroyedCount = 2;

        [Header("패턴 쿨타임")]
        [SerializeField] private float _cooldownMin = 2f;
        [SerializeField] private float _cooldownMax = 4f;

        [Header("Phase 1 패턴 목록")]
        [SerializeField] private BossPattern[] _phase1Patterns;

        [Header("Phase 2 패턴 목록")]
        [SerializeField] private BossPattern[] _phase2Patterns;

        [Header("Phase2 전환 연출")]
        [Tooltip("Phase2 전환 시 재생할 Animator 트리거 이름. 비워두면 생략.")]
        [SerializeField] private string _phase2TransitionTrigger = "";

        [Tooltip("Phase2 전환 연출 대기 시간 (초)")]
        [SerializeField] private float _phase2TransitionDuration = 1f;

        [Header("상시 패턴 (보스 등장 시 시작 · 팔 등 항상 유지되는 패턴)")]
        [SerializeField] private BossCustomPatternBase[] _passivePatterns;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        private Animator      _animator;
        private BossMainPhase _phase          = BossMainPhase.Phase1;
        private BossMainState _state          = BossMainState.Idle;
        private int           _destroyedCount = 0;
        private int           _lastPatternIdx = -1;
        private bool          _phase2Triggered;
        private Coroutine     _patternLoopHandle;

        private Dictionary<string, BossCustomPatternBase> _customPatterns;

        // ── IStatusLockable ───────────────────────────────────────────────
        private int  _actionLockCount;
        private int  _frozenCount;
        private bool IsActionLocked => _actionLockCount > 0;
        public  bool IsFrozen       => _frozenCount > 0;

        // ── 생명주기 ─────────────────────────────────────────────────────

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            // 자식 컴포넌트에서 커스텀 패턴 수집
            var customs = GetComponentsInChildren<BossCustomPatternBase>();
            _customPatterns = new Dictionary<string, BossCustomPatternBase>(customs.Length);
            foreach (var c in customs)
                _customPatterns[c.PatternId] = c;

            if (_parts == null) return;
            foreach (var part in _parts)
                if (part != null)
                    part.OnPartDestroyed += HandlePartDestroyed;
        }

        private void OnEnable()
        {
            // EnemySpawner 대신 직접 등록 — EnemySpawner는 EnemyStats만 탐색하므로
            StageManager.Instance?.RegisterEnemy();
            ResetBoss();
            StartCoroutine(EngageNextFrame());
        }

        private IEnumerator EngageNextFrame()
        {
            // BossRoomManager.OnEnable 구독 완료 후 이벤트 발생 보장
            yield return null;
            OnBossEngaged?.Invoke();

            if (_passivePatterns != null)
                foreach (var p in _passivePatterns)
                    p?.BeginExecute();

            _patternLoopHandle = StartCoroutine(PatternLoop());
        }

        private void OnDisable()
        {
            if (_patternLoopHandle != null)
            {
                StopCoroutine(_patternLoopHandle);
                _patternLoopHandle = null;
            }

            if (_passivePatterns != null)
                foreach (var p in _passivePatterns)
                    p?.Cancel();
        }

        private void OnDestroy()
        {
            if (_parts == null) return;
            foreach (var part in _parts)
                if (part != null)
                    part.OnPartDestroyed -= HandlePartDestroyed;
        }

        // ── 보스 초기화 ───────────────────────────────────────────────────

        private void ResetBoss()
        {
            _destroyedCount  = 0;
            _phase           = BossMainPhase.Phase1;
            _phase2Triggered = false;
            _state           = BossMainState.Idle;
            _lastPatternIdx  = -1;

            if (_parts != null)
                foreach (var part in _parts)
                    part?.Reset();
        }

        // ── 파츠 파괴 처리 ────────────────────────────────────────────────

        private void HandlePartDestroyed()
        {
            _destroyedCount++;

            if (!_phase2Triggered && _destroyedCount >= _phase2DestroyedCount)
                StartCoroutine(TransitionToPhase2());

            if (_parts != null && _destroyedCount >= _parts.Length)
                HandleBossDeath();
        }

        // ── 페이즈 전환 ───────────────────────────────────────────────────

        private IEnumerator TransitionToPhase2()
        {
            _phase2Triggered = true;
            _phase           = BossMainPhase.Phase2;

            if (_patternLoopHandle != null)
            {
                StopCoroutine(_patternLoopHandle);
                _patternLoopHandle = null;
            }

            _state = BossMainState.Idle;

            if (!string.IsNullOrEmpty(_phase2TransitionTrigger))
                _animator?.SetTrigger(Animator.StringToHash(_phase2TransitionTrigger));

            yield return StartCoroutine(PauseableWait(_phase2TransitionDuration));

            if (_state != BossMainState.Dead)
                _patternLoopHandle = StartCoroutine(PatternLoop());
        }

        // ── 패턴 루프 ────────────────────────────────────────────────────

        private IEnumerator PatternLoop()
        {
            while (_state != BossMainState.Dead)
            {
                _state = BossMainState.Idle;

                yield return StartCoroutine(PauseableWait(Random.Range(_cooldownMin, _cooldownMax)));

                if (_state == BossMainState.Dead) yield break;

                var pool = _phase == BossMainPhase.Phase2 ? _phase2Patterns : _phase1Patterns;
                if (pool == null || pool.Length == 0) continue;

                int idx = SelectPattern(pool);
                _lastPatternIdx = idx;

                _state = BossMainState.ExecutingPattern;
                yield return StartCoroutine(ExecutePattern(pool[idx]));

                _state = BossMainState.Recovery;
                yield return StartCoroutine(PauseableWait(pool[idx].recoveryTime));
            }
        }

        private int SelectPattern(BossPattern[] pool)
        {
            if (pool.Length == 1) return 0;

            int idx;
            int attempts = 0;
            do
            {
                idx = Random.Range(0, pool.Length);
                attempts++;
            }
            while (idx == _lastPatternIdx && attempts < 10);

            return idx;
        }

        private IEnumerator ExecutePattern(BossPattern pattern)
        {
            // 커스텀 패턴 분기 — steps 없이 patternName 만 지정한 경우
            if ((pattern.steps == null || pattern.steps.Length == 0)
                && !string.IsNullOrEmpty(pattern.patternName)
                && _customPatterns != null
                && _customPatterns.TryGetValue(pattern.patternName, out var custom))
            {
                yield return custom.BeginExecute();
                yield break;
            }

            if (!string.IsNullOrEmpty(pattern.patternName))
                Debug.LogWarning($"[Boss] 패턴 '{pattern.patternName}' 을 찾지 못함. 커스텀패턴 수={_customPatterns?.Count ?? -1}");

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

        private void ApplyAnimatorCommand(AnimatorCommand cmd)
        {
            if (_animator == null || string.IsNullOrEmpty(cmd.paramName)) return;

            int hash = Animator.StringToHash(cmd.paramName);
            switch (cmd.type)
            {
                case AnimatorCommand.CmdType.Trigger:  _animator.SetTrigger(hash);        break;
                case AnimatorCommand.CmdType.BoolOn:   _animator.SetBool(hash, true);     break;
                case AnimatorCommand.CmdType.BoolOff:  _animator.SetBool(hash, false);    break;
            }
        }

        // ── 보스 사망 처리 ────────────────────────────────────────────────

        private void HandleBossDeath()
        {
            if (_state == BossMainState.Dead) return;
            _state = BossMainState.Dead;

            if (_patternLoopHandle != null)
            {
                StopCoroutine(_patternLoopHandle);
                _patternLoopHandle = null;
            }

            // 진행 중인 커스텀 패턴 강제 종료
            if (_customPatterns != null)
                foreach (var c in _customPatterns.Values)
                    c.Cancel();

            // 상시 패턴 종료
            if (_passivePatterns != null)
                foreach (var p in _passivePatterns)
                    p?.Cancel();

            StageManager.Instance?.OnEnemyDied();
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
                if (_customPatterns != null)
                    foreach (var c in _customPatterns.Values)
                        c.Cancel();
                _state = BossMainState.Idle;
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

            if (!IsActionLocked && _patternLoopHandle == null && _state != BossMainState.Dead)
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
