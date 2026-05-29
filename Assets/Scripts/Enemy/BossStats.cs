using System;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 전용 체력/데미지 수신 시스템.
    /// EnemyStats와 달리 오브젝트 풀에 반납되지 않으며,
    /// HP 임계값 도달 시 Phase 2 진입 이벤트, 사망 시 보스 사망 이벤트를 발행한다.
    /// </summary>
    public class BossStats : MonoBehaviour, IDamageable, IDotReceiver
    {
        [Header("데이터 SO")]
        [SerializeField] private BossStatsSO _data;

        [Header("스탯")]
        [SerializeField] private float _maxHp = 1000f;
        [Tooltip("Phase 2 진입 HP 비율 (0.5 = 50%)")]
        [SerializeField] [Range(0.01f, 0.99f)] private float _phase2HpRatio = 0.5f;

        [Header("데미지 텍스트")]
        [SerializeField] private Transform _damageSpawnPos;

        [Header("피격 사운드")]
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _heavyHitClips;

        private float _currentHp;
        private bool  _isDead;
        private bool  _isPhase2Entered;
        private bool  _isInvincible;

        private Animator          _animator;
        private DamageFlash       _damageFlash;
        private HitEffectSpawner  _hitEffectSpawner;
        private StatusController  _statusController;

        // ── 외부 구독 이벤트 ──────────────────────────────────────────────
        /// <summary>HP가 Phase 2 임계값 이하로 내려갈 때 1회 발행</summary>
        public static event Action OnBossPhase2Enter;
        /// <summary>보스 사망 확정 시 발행</summary>
        public static event Action OnBossDeadEvent;

        // ── IDamageable ───────────────────────────────────────────────────
        public bool IsDead       => _isDead;
        public bool IsInvincible => _isInvincible;

        // ── 외부 참조용 프로퍼티 (HP 바 연동 등) ─────────────────────────
        public float CurrentHp => _currentHp;
        public float MaxHp     => _maxHp;
        public float HpRatio   => _maxHp > 0f ? _currentHp / _maxHp : 0f;

        private static readonly int AnimDie = Animator.StringToHash("Die");
        private static readonly int AnimHit = Animator.StringToHash("Hit");

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data != null)
            {
                _maxHp         = _data.MaxHp;
                _phase2HpRatio = _data.Phase2HpRatio;
                _hitClips      = _data.HitClips;
                _heavyHitClips = _data.HeavyHitClips;
            }
            _currentHp        = _maxHp;
            _animator         = GetComponent<Animator>();
            _damageFlash      = GetComponent<DamageFlash>();
            _statusController = GetComponent<StatusController>();
        }

        private void Start()
        {
            _hitEffectSpawner = HitEffectSpawner.Instance;
        }

        // ── 공개 메서드 ───────────────────────────────────────────────────

        /// <summary>Phase 2 연출 진행 중 데미지를 차단한다.</summary>
        public void SetInvincible(bool invincible) => _isInvincible = invincible;

        // ── IDamageable 구현 ──────────────────────────────────────────────

        public void TakeDamage(HitInfo info)
        {
            if (_isDead)       return;
            if (_isInvincible && !info.IgnoreInvincibility) return;

            _currentHp = Mathf.Max(0f, _currentHp - info.Damage);
            SpawnDamageText(info.Damage);

            // 피격 이벤트 먼저 전파 (빙결 해제 등이 새 상태이상 적용보다 선행해야 함)
            _statusController?.OnHitReceived(info);
            if (info.StatusEffects != null && _statusController != null)
                foreach (var spec in info.StatusEffects)
                    _statusController.ApplyStatus(spec);

            CheckPhase2Transition();

            if (_currentHp <= 0f)
            {
                _isDead = true;
                HandleDeath();
            }
            else
            {
                _damageFlash?.CallDamageFlash();
                SafeSetTrigger(AnimHit);
                _hitEffectSpawner?.Spawn(transform.position);
                HitSoundAggregator.Instance?.RegisterHit(
                    info.AttackId, transform.position, _hitClips, _heavyHitClips);
            }
        }

        /// <summary>넉백·피격 애니메이션 없이 체력만 감소시키는 DoT 전용 처리.</summary>
        public void TakeDotDamage(float amount)
        {
            if (_isDead || _isInvincible) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            SpawnDamageText(amount, FloatingTextType.StatusEffect);

            CheckPhase2Transition();

            if (_currentHp <= 0f)
            {
                _isDead = true;
                HandleDeath();
            }
        }

        // ── 내부 처리 ─────────────────────────────────────────────────────

        /// <summary>Phase 2 진입 조건 체크 — 중복 발행 방지</summary>
        private void CheckPhase2Transition()
        {
            if (_isPhase2Entered) return;
            if (_currentHp > _maxHp * _phase2HpRatio) return;

            _isPhase2Entered = true;
            OnBossPhase2Enter?.Invoke();
        }

        private void HandleDeath()
        {
            SafeSetTrigger(AnimDie);
            OnBossDeadEvent?.Invoke();
        }

        private void SpawnDamageText(float amount, FloatingTextType type = FloatingTextType.Damage)
        {
            if (FloatingTextSpawner.Instance == null) return;
            var pos = _damageSpawnPos != null
                ? _damageSpawnPos.position
                : transform.position + new Vector3(0f, 1f, 0f);
            FloatingTextSpawner.Instance.Spawn(pos, Mathf.RoundToInt(amount).ToString(), type);
        }

        /// <summary>파라미터가 없는 경우 조용히 무시 (EnemyStats 동일 방식)</summary>
        private void SafeSetTrigger(int hash)
        {
            if (_animator == null) return;
            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == hash)
                {
                    _animator.SetTrigger(hash);
                    return;
                }
            }
        }

        // ── 에디터 시각화 ─────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Phase 2 진입 HP 시각화 (씬뷰 선택 시)
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Phase2 at {_phase2HpRatio * 100f:F0}% HP");
        }
#endif
    }
}
