using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 하수인 공통 기반 클래스.
    /// BossPatternExecutor가 스폰 후 InitializeMinion()을 호출하면,
    /// 포탈 전조 이펙트를 재생하고 지정 방향으로 AttackCoroutine을 실행한다.
    /// </summary>
    [RequireComponent(typeof(EnemyStats))]
    public abstract class MinionBase : MonoBehaviour
    {
        [Header("공통")]
        [Tooltip("포탈 전조 이펙트 프리팹 (SkillEffectActor 또는 ParticleSystem)")]
        [SerializeField] protected GameObject _portalEffectPrefab;
        [Tooltip("포탈 이펙트 재생 후 행동 시작까지 대기 시간 (초)")]
        [SerializeField] protected float _predelay = 1f;

        protected int        _facingDir = 1;    // 1 = 오른쪽, -1 = 왼쪽
        protected Transform  _player;
        protected EnemyStats _stats;

        private Coroutine _lifetimeHandle;

        // ── 생명주기 ──────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _stats = GetComponent<EnemyStats>();
        }

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;
        }

        /// <summary>
        /// BossPatternExecutor가 스폰 직후 호출한다.
        /// facingDir: 1 = 오른쪽, -1 = 왼쪽
        /// </summary>
        public void InitializeMinion(int facingDir)
        {
            _facingDir = facingDir;
            ApplyFacing();
            _lifetimeHandle = StartCoroutine(LifetimeCoroutine());
        }

        /// <summary>외부(BossPatternExecutor)에서 패턴 종료 시 강제 정리 호출</summary>
        public void ForceCleanup()
        {
            if (_lifetimeHandle != null)
            {
                StopCoroutine(_lifetimeHandle);
                _lifetimeHandle = null;
            }
            OnForceCleanup();
            Destroy(gameObject);
        }

        // ── 내부 흐름 ─────────────────────────────────────────────────────

        private IEnumerator LifetimeCoroutine()
        {
            // 포탈 전조 이펙트 재생
            if (_portalEffectPrefab != null)
                Instantiate(_portalEffectPrefab, transform.position, Quaternion.identity);

            yield return new WaitForSeconds(_predelay);

            // 사망 전 패턴 실행
            if (!_stats.IsDead)
                yield return StartCoroutine(AttackCoroutine());

            // 패턴 종료 후 자연 소멸 (EnemyStats.OnDead가 아직 처리 안 된 경우)
            if (!_stats.IsDead)
                Destroy(gameObject);
        }

        // ── 서브클래스 구현 ───────────────────────────────────────────────

        /// <summary>하수인 고유 공격 패턴. 코루틴이 완료되면 하수인이 소멸한다.</summary>
        protected abstract IEnumerator AttackCoroutine();

        /// <summary>ForceCleanup 호출 시 서브클래스가 처리해야 할 정리 작업 (이펙트 오브젝트 제거 등)</summary>
        protected virtual void OnForceCleanup() { }

        // ── 유틸 ─────────────────────────────────────────────────────────

        protected void ApplyFacing()
        {
            Vector3 scale = transform.localScale;
            scale.x = _facingDir > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
