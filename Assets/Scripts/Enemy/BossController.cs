using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public enum BossPhase { Phase1, Phase2Transition, Phase2, Dead }

    /// <summary>
    /// 보스 전체 흐름을 관장하는 컨트롤러.
    /// 이 보스는 움직이지 않으며, Phase 1/2 상태 전환과 패턴 루프 시작/중단을 담당한다.
    /// 실제 패턴 코루틴은 BossPatternExecutor에 위임한다.
    /// </summary>
    [RequireComponent(typeof(BossStats))]
    [RequireComponent(typeof(BossPatternExecutor))]
    public class BossController : MonoBehaviour, IStatusLockable
    {
        // ── 참조 ──────────────────────────────────────────────────────────
        [Header("컴포넌트 참조")]
        [SerializeField] private Animator _animator;

        [Header("Phase 2 — 연출")]
        [Tooltip("Phase 2 전환 시 보스 아래 스폰할 오벨리스크 프리팹 (생존 판정용)")]
        [SerializeField] private GameObject _transitionObeliskPrefab;
        [Tooltip("오벨리스크 스폰 위치 (보통 보스 발 아래)")]
        [SerializeField] private Transform  _transitionObeliskSpawnPoint;
        [Tooltip("오벨리스크 생존 반경 — 이 범위 밖 플레이어를 사망시킨다")]
        [SerializeField] private float      _survivalRadius = 3f;
        [Tooltip("화면 흔들림 지속 시간 (초)")]
        [SerializeField] private float      _screenShakeDuration  = 0.6f;
        [Tooltip("화면 흔들림 강도")]
        [SerializeField] private float      _screenShakeIntensity = 0.25f;
        [Tooltip("오벨리스크 등장 후 지형 변경까지 대기 시간 (초)")]
        [SerializeField] private float      _terrainChangDelay    = 4f;

        [Header("Phase 2 — 플레이어 제한")]
        [Tooltip("Phase 2 동안 플레이어 대쉬 최대 횟수")]
        [SerializeField] private int   _phase2MaxDashes = 1;

        [Header("Phase 2 — 플랫폼")]
        [SerializeField] private PlatformManager _platformManager;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        private BossPhase          _phase = BossPhase.Phase1;
        private BossStats          _stats;
        private BossPatternExecutor _patternExecutor;

        private PlayerController   _playerController;
        private PlayerDash         _playerDash;
        private PlayerStats        _playerStats;

        private int  _actionLockCount;
        private int  _frozenCount;
        private bool IsActionLocked => _actionLockCount > 0;
        private bool IsFrozen       => _frozenCount      > 0;

        // ── Animator 해시 ─────────────────────────────────────────────────
        private static readonly int AnimDie   = Animator.StringToHash("Die");
        private static readonly int AnimPhase2 = Animator.StringToHash("Phase2");

        // ── 공개 이벤트 ──────────────────────────────────────────────────
        /// <summary>보스 전투 시작 시 발행 (방 잠금 등 스테이지 연동용)</summary>
        public static event System.Action OnBossEngaged;
        /// <summary>보스 사망 확정 시 발행 (보상 처리 등 스테이지 연동용)</summary>
        public static event System.Action OnBossDead;

        // ── 공개 프로퍼티 ─────────────────────────────────────────────────
        public BossPhase CurrentPhase => _phase;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            _stats           = GetComponent<BossStats>();
            _patternExecutor = GetComponent<BossPatternExecutor>();
        }

        private void Start()
        {
            // 플레이어 캐싱
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerController = playerGO.GetComponent<PlayerController>();
                _playerDash       = playerGO.GetComponent<PlayerDash>();
                _playerStats      = playerGO.GetComponent<PlayerStats>();
            }

            // BossStats 이벤트 구독
            BossStats.OnBossPhase2Enter += HandlePhase2Enter;
            BossStats.OnBossDeadEvent   += HandleBossDead;

            OnBossEngaged?.Invoke();

            // Phase 1 패턴 루프 시작
            _patternExecutor.StartPhase1Loop();
        }

        private void OnDestroy()
        {
            BossStats.OnBossPhase2Enter -= HandlePhase2Enter;
            BossStats.OnBossDeadEvent   -= HandleBossDead;
        }

        // ── Phase 전환 ────────────────────────────────────────────────────

        private void HandlePhase2Enter()
        {
            if (_phase != BossPhase.Phase1) return;
            _phase = BossPhase.Phase2Transition;
            StartCoroutine(Phase2TransitionCoroutine());
        }

        private void HandleBossDead()
        {
            if (_phase == BossPhase.Dead) return;
            _phase = BossPhase.Dead;

            StopAllPatterns();
            RestorePlayerAbilities();

            _platformManager?.DisablePlatforms();
            _animator?.SetTrigger(AnimDie);

            OnBossDead?.Invoke();
        }

        // ── Phase 2 전환 연출 코루틴 ─────────────────────────────────────

        private IEnumerator Phase2TransitionCoroutine()
        {
            // ① 패턴 중단 + 무적 활성화
            StopAllPatterns();
            _stats.SetInvincible(true);

            // ② 화면 흔들림
            yield return StartCoroutine(ScreenShakeCoroutine(_screenShakeDuration, _screenShakeIntensity));

            // ③ 보스 아래 오벨리스크 생성 (생존 판정용)
            GameObject transObelisk = null;
            if (_transitionObeliskPrefab != null && _transitionObeliskSpawnPoint != null)
                transObelisk = Instantiate(_transitionObeliskPrefab, _transitionObeliskSpawnPoint.position, Quaternion.identity);

            _animator?.SetTrigger(AnimPhase2);

            // ④ 플레이어에게 대피 시간 부여
            yield return new WaitForSeconds(_terrainChangDelay);

            // ⑤ 오벨리스크 주변에 없는 플레이어 사망 처리
            if (_playerStats != null && !_playerStats.IsDead && _transitionObeliskSpawnPoint != null)
            {
                float dist = Vector2.Distance(_playerStats.transform.position, _transitionObeliskSpawnPoint.position);
                if (dist > _survivalRadius)
                    KillPlayer();
            }

            // ⑥ 전환용 오벨리스크 제거
            if (transObelisk != null)
                Destroy(transObelisk);

            // ⑦ 플랫폼 활성화 + 플레이어 이동
            _platformManager?.ActivatePlatforms();
            _platformManager?.TeleportPlayerToPlatform(0);

            // ⑧ 플레이어 능력 제한
            ApplyPhase2PlayerRestrictions();

            // ⑨ 무적 해제 + Phase 2 패턴 루프 시작
            _stats.SetInvincible(false);
            _phase = BossPhase.Phase2;
            _patternExecutor.StartPhase2Loop();
        }

        // ── 플레이어 능력 제한 ────────────────────────────────────────────

        private void ApplyPhase2PlayerRestrictions()
        {
            _playerController?.SetJumpEnabled(false);
            _playerDash?.SetMaxCharges(_phase2MaxDashes);
        }

        private void RestorePlayerAbilities()
        {
            _playerController?.SetJumpEnabled(true);
            _playerDash?.RestoreMaxCharges();
        }

        private void KillPlayer()
        {
            if (_playerStats == null) return;
            // IgnoreInvincibility로 확실히 처리
            _playerStats.TakeDamage(new HitInfo
            {
                Damage              = 99999f,
                DamageType          = DamageType.Magic,
                SourcePosition      = transform.position,
                KnockbackForce      = 0f,
                IgnoreInvincibility = true,
            });
        }

        // ── 패턴 정리 ─────────────────────────────────────────────────────

        /// <summary>진행 중인 모든 패턴과 생성된 오브젝트를 정리한다.</summary>
        public void StopAllPatterns()
        {
            _patternExecutor.StopAll();
        }

        // ── 화면 흔들림 ───────────────────────────────────────────────────

        /// <summary>
        /// 카메라를 흔드는 단순 구현.
        /// Cinemachine 등 별도 카메라 시스템 사용 시 이 코루틴을 교체할 것.
        /// </summary>
        private IEnumerator ScreenShakeCoroutine(float duration, float intensity)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 originalPos = cam.transform.localPosition;
            float   elapsed     = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cam.transform.localPosition = originalPos;
        }

        // ── IStatusLockable ───────────────────────────────────────────────

        public void ApplyActionLock(bool cancelOngoing)
        {
            _actionLockCount++;
            if (cancelOngoing)
            {
                StopAllPatterns();
                _phase = BossPhase.Phase1; // 스턴 해제 후 루프를 재개할 기준 상태 보존
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

            // 잠금 해제 후 Phase에 맞는 루프 재개
            if (!IsActionLocked && _phase == BossPhase.Phase1)
                _patternExecutor.StartPhase1Loop();
            else if (!IsActionLocked && _phase == BossPhase.Phase2)
                _patternExecutor.StartPhase2Loop();
        }

        // ── Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_transitionObeliskSpawnPoint == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawWireSphere(_transitionObeliskSpawnPoint.position, _survivalRadius);
            UnityEditor.Handles.Label(
                _transitionObeliskSpawnPoint.position + Vector3.up * (_survivalRadius + 0.2f),
                $"생존 반경 {_survivalRadius}m");
        }
#endif
    }
}
