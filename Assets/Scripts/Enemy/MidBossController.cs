using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 중간 보스 AI.
    /// EnemyBrainBase의 기본 이동·순찰·감지를 그대로 사용하며,
    /// 10초 쿨타임마다 점프 후 내려찍기 + 박스 범위 공격 3연속을 시전한다.
    /// </summary>
    public class MidBossController : EnemyBrainBase
    {
        // ── 근접 공격 ─────────────────────────────────────────────────────
        [Header("근접 공격")]
        [SerializeField] private float _attackDamage   = 15f;
        [SerializeField] private float _knockbackForce = 5f;

        // ── 내려찍기 스킬 ─────────────────────────────────────────────────
        [Header("내려찍기 스킬")]
        [Tooltip("박스형 AreaSkillSpec SO를 연결한다")]
        [SerializeField] private AreaSkillSpec _slamSpec;

        [Tooltip("스킬 재사용 대기시간 (초)")]
        [SerializeField] private float _slamCooldown  = 10f;

        [Tooltip("점프 초기 힘 (Impulse)")]
        [SerializeField] private float _jumpForce     = 14f;

        [Tooltip("공중 체공 시간 (초) — 이 후 강제 하강")]
        [SerializeField] private float _jumpHangTime  = 0.5f;

        [Tooltip("강제 하강 속도 (양수 입력, 아래로 이동)")]
        [SerializeField] private float _slamDownSpeed = 20f;

        [Tooltip("착지 후 내려찍기 모션 선딜 (초)")]
        [SerializeField] private float _slamLandTime  = 0.25f;

        [Tooltip("박스 간 X 간격 (m) — 보스 정면 방향 기준")]
        [SerializeField] private float _boxStep       = 1.5f;

        [Tooltip("박스 간 발동 딜레이 (초)")]
        [SerializeField] private float _boxInterval   = 0.3f;

        [Tooltip("스킬 후딜 (초)")]
        [SerializeField] private float _slamEndLag    = 0.3f;

        // ── 슬램 이펙트 ───────────────────────────────────────────────────
        [Header("슬램 이펙트")]
        [Tooltip("SkillEffectActor가 붙은 이펙트 프리팹")]
        [SerializeField] private GameObject _slamEffectPrefab;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        private AreaSkillExecutor _areaExecutor;
        private float             _slamTimer;   // 마지막 슬램 이후 경과 시간

        // ── 애니메이터 해시 ───────────────────────────────────────────────
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimJump   = Animator.StringToHash("Jump");
        private static readonly int AnimSlam   = Animator.StringToHash("Slam");

        // ── 초기화 ────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _areaExecutor = GetComponent<AreaSkillExecutor>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _slamTimer = 0f;   // 풀 반환 후 재사용 시 쿨타임 초기화
        }

        // ── AI 루프 ───────────────────────────────────────────────────────

        /// <summary>
        /// 슬램 쿨타임을 여기서 누적한다.
        /// 넉백·기절·공격 중에는 HandleAI()가 호출되지 않으므로 타이머도 정지된다.
        /// </summary>
        protected override void HandleAI()
        {
            _slamTimer += Time.deltaTime;

            if (_slamTimer >= _slamCooldown && _canAttack)
            {
                _slamTimer    = 0f;
                _canAttack    = false;
                _isAttacking  = true;
                _attackHandle = StartCoroutine(SlamSkillCoroutine());
                return;
            }

            base.HandleAI();
        }

        // ── 근접 공격 ─────────────────────────────────────────────────────

        protected override IEnumerator AttackCoroutine()
        {
            _canAttack   = false;
            _isAttacking = true;
            _animator?.SetTrigger(AnimAttack);

            yield return StartCoroutine(PauseableWait(0.25f));

            if (_player != null && Vector2.Distance(transform.position, _player.position) <= _attackRange)
            {
                _player.GetComponent<IDamageable>()?.TakeDamage(new HitInfo
                {
                    Damage         = _attackDamage,
                    SourcePosition = transform.position,
                    KnockbackForce = _knockbackForce,
                });
            }

            yield return new WaitForSeconds(_attackCooldown - 0.25f);
            _isAttacking = false;
            _canAttack   = true;
        }

        // ── 내려찍기 스킬 ─────────────────────────────────────────────────

        private IEnumerator SlamSkillCoroutine()
        {
            // ① 점프
            _animator?.SetTrigger(AnimJump);
            _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);

            yield return StartCoroutine(PauseableWait(_jumpHangTime));

            // ② 강제 하강 — 수평 속도는 유지하지 않고 수직만 설정
            _rb.linearVelocity = new Vector2(0f, -_slamDownSpeed);

            // ③ 착지 대기 (고정 타이머, IsGrounded 미사용)
            yield return new WaitForSeconds(0.25f);

            // ④ 착지 고정 + 슬램 모션
            _rb.linearVelocity = Vector2.zero;
            _animator?.SetTrigger(AnimSlam);

            yield return StartCoroutine(PauseableWait(_slamLandTime));

            // ⑤ 박스 3개 순차 발동
            float   dir     = transform.localScale.x >= 0f ? 1f : -1f;
            Vector2 forward = new Vector2(dir, 0f);

            for (int i = 0; i < 3; i++)
            {
                Vector2 origin = (Vector2)transform.position
                               + new Vector2(dir * _boxStep * (i + 1), 0f);

                // 이펙트 스폰 — 판정보다 먼저 띄워 시각 피드백 제공
                if (_slamEffectPrefab != null)
                    Instantiate(_slamEffectPrefab, origin, Quaternion.identity);

                _areaExecutor?.Execute(_slamSpec, origin, forward);

                yield return StartCoroutine(PauseableWait(_boxInterval));
            }

            // ⑥ 후딜
            yield return new WaitForSeconds(_slamEndLag);

            _isAttacking  = false;
            _canAttack    = true;
            _attackHandle = null;
        }

        // ── Gizmo ─────────────────────────────────────────────────────────

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (_slamSpec == null || _slamSpec.ShapeType != AreaShapeType.Box) return;

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            float dir = transform.localScale.x >= 0f ? 1f : -1f;

            for (int i = 0; i < 3; i++)
            {
                Vector3 origin = transform.position + new Vector3(dir * _boxStep * (i + 1), 0f, 0f);
                Gizmos.DrawWireCube(origin, _slamSpec.BoxSize);
            }
        }
    }
}
