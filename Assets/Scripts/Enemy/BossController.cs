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
        [Tooltip("화면 흔들림 초기 지속 시간 — 이 시간 후 오벨리스크가 스폰됨. 흔들림 자체는 오벨리스크 제거까지 지속")]
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

        [Header("지형 전환")]
        [Tooltip("Phase 1 바닥 루트 오브젝트 — Phase 2 전환 시 비활성화, 보스 사망 시 재활성화")]
        [SerializeField] private GameObject _phase1Root;
        [Tooltip("보스 사망 후 플레이어를 이동시킬 스폰 포인트 (StageManager spawnPoint와 동일 Transform)")]
        [SerializeField] private Transform  _spawnPoint_Boss;

        [Header("카메라")]
        [Tooltip("보스전 중 카메라를 고정할 중앙 위치 (없으면 보스 자신의 위치 사용)")]
        [SerializeField] private Transform _bossStageCenterPoint;
        [Tooltip("보스전 중 카메라 orthographic size — 기본값보다 클수록 더 넓게 보임")]
        [SerializeField] private float     _bossOrthoSize            = 10f;
        [Tooltip("카메라 줌·위치 전환 시간 (초)")]
        [SerializeField] private float     _cameraTransitionDuration = 1.5f;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        private BossPhase           _phase = BossPhase.Phase1;
        private BossStats           _stats;
        private BossPatternExecutor _patternExecutor;
        private CameraFollow        _camFollow;

        private PlayerController   _playerController;
        private PlayerDash         _playerDash;
        private PlayerStats        _playerStats;

        private int  _actionLockCount;
        private int  _frozenCount;
        private bool IsActionLocked => _actionLockCount > 0;
        private bool IsFrozen       => _frozenCount      > 0;

        // ── Animator 해시 ─────────────────────────────────────────────────
        private static readonly int AnimDie    = Animator.StringToHash("Die");
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

            // 카메라 캐싱 + 보스전 진입 연출
            _camFollow = Camera.main?.GetComponent<CameraFollow>();
            if (_camFollow != null)
            {
                Vector3 center = _bossStageCenterPoint != null
                    ? _bossStageCenterPoint.position
                    : transform.position;
                _camFollow.SetFixedWorldPos(center);
                _camFollow.ZoomTo(_bossOrthoSize, _cameraTransitionDuration);
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

            // 바닥 복원 → 플레이어 복귀 (순서 중요: 바닥 먼저 활성화한 뒤 이동)
            if (_phase1Root != null) _phase1Root.SetActive(true);
            if (_spawnPoint_Boss != null && _playerStats != null)
                _playerStats.transform.position = _spawnPoint_Boss.position;

            _animator?.SetTrigger(AnimDie);

            OnBossDead?.Invoke();

            // 카메라 플레이어 추적 복귀
            if (_camFollow != null)
            {
                _camFollow.ClearFixedWorldPos();
                _camFollow.ResetZoom(_cameraTransitionDuration);
            }
        }

        // ── Phase 2 전환 연출 코루틴 ─────────────────────────────────────

        private IEnumerator Phase2TransitionCoroutine()
        {
            // ① 패턴 중단 + 무적 활성화
            StopAllPatterns();
            _stats.SetInvincible(true);

            // ② 화면 흔들림 시작 — 오벨리스크가 제거될 때까지 지속 (총 duration = 초기 딜레이 + 대피 시간)
            _camFollow?.Shake(_screenShakeDuration + _terrainChangDelay, _screenShakeIntensity);
            yield return new WaitForSeconds(_screenShakeDuration);

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

            // ⑥ 전환용 오벨리스크 제거 — 이 시점에 흔들림도 자연스럽게 종료
            if (transObelisk != null)
                Destroy(transObelisk);

            // ⑦ Phase1 바닥 비활성화 → 플랫폼 활성화 + 플레이어 이동
            if (_phase1Root != null) _phase1Root.SetActive(false);
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

        // ── IStatusLockable ───────────────────────────────────────────────

        public void ApplyActionLock(bool cancelOngoing)
        {
            _actionLockCount++;
            if (cancelOngoing)
            {
                StopAllPatterns();
                _phase = BossPhase.Phase1;
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
