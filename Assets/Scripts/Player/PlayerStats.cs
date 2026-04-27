using UnityEngine;

namespace _2D_Roguelike
{
    public class PlayerStats : MonoBehaviour, IDamageable, IDotReceiver
    {
        [SerializeField] private float     _maxHp           = 100f;
        [SerializeField] private Transform _damageSpawnPos;  // 플레이어 머리 위 빈 Transform (없으면 중심 + offset 사용)

        private float                _currentHp;
        private Animator             _animator;
        private DamageFlash          _damageFlash;
        private KnockbackReceiver    _knockback;
        private InvincibilityHandler _invincibility;
        private PlayerStatController _statController;
        private StatusController     _statusController;

        private static readonly int AnimIsHurt = Animator.StringToHash("IsHurt");

        public float CurrentHp    => _currentHp;
        /// <summary>아이템/각인 효과가 반영된 최대 체력</summary>
        public float MaxHp        => _statController != null
                                        ? _statController.StatService.GetFinalValue(StatType.MaxHp)
                                        : _maxHp;
        public bool  IsDead       => _currentHp <= 0f;
        public bool  IsInvincible => _invincibility != null && _invincibility.IsInvincible;

        private void Awake()
        {
            _animator         = GetComponent<Animator>();
            _damageFlash      = GetComponent<DamageFlash>();
            _knockback        = GetComponent<KnockbackReceiver>();
            _invincibility    = GetComponent<InvincibilityHandler>();
            _statController   = GetComponent<PlayerStatController>();
            _statusController = GetComponent<StatusController>();
        }

        private void Start()
        {
            // Inspector 수치를 기본값으로 StatService에 등록
            _statController?.StatService.SetBaseValue(StatType.MaxHp, _maxHp);
            _currentHp = MaxHp;
        }

        public void FullRestore()
        {
            _currentHp = MaxHp;
        }

        public void TakeDamage(HitInfo info)
        {
            if (IsDead) return;
            if (IsInvincible && !info.IgnoreInvincibility) return;

            _currentHp = Mathf.Max(0f, _currentHp - info.Damage);
            Debug.Log($"[PlayerStats] HP: {_currentHp}/{_maxHp}");

            SpawnFloatingText(info.Damage, FloatingTextType.Damage);
            _damageFlash?.CallDamageFlash();
            _animator?.SetTrigger(AnimIsHurt);

            if (info.KnockbackForce > 0f)
                _knockback?.ApplyKnockback(info.SourcePosition, info.KnockbackForce);

            // 상태이상 적용 (StatusResistance가 기절·빙결 면역을 자동 처리)
            if (info.StatusEffects != null && _statusController != null)
            {
                foreach (var spec in info.StatusEffects)
                    _statusController.ApplyStatus(spec);
            }

            if (IsDead)
                OnDead();
        }

        /// <summary>
        /// DoT 전용 데미지 처리.
        /// 넉백·무적·피격 애니메이션 없이 체력만 깎는다.
        /// </summary>
        public void TakeDotDamage(float amount)
        {
            if (IsDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            SpawnFloatingText(amount, FloatingTextType.StatusEffect);

            if (IsDead)
                OnDead();
        }

        /// <summary>플레이어 회복 처리. 초록 숫자 텍스트 출력</summary>
        public void Heal(float amount)
        {
            if (IsDead) return;

            float actual = Mathf.Min(amount, _maxHp - _currentHp);  // 최대 체력 초과 방지
            if (actual <= 0f) return;

            _currentHp += actual;
            Debug.Log($"[PlayerStats] Heal +{actual}  HP: {_currentHp}/{_maxHp}");

            SpawnFloatingText(actual, FloatingTextType.Heal);
        }

        private void SpawnFloatingText(float amount, FloatingTextType type)
        {
            if (FloatingTextSpawner.Instance == null) return;
            var pos = _damageSpawnPos != null
                ? _damageSpawnPos.position
                : transform.position + new Vector3(0f, 1.0f, 0f);
            var text = type == FloatingTextType.Heal
                ? "+" + Mathf.RoundToInt(amount)
                : Mathf.RoundToInt(amount).ToString();
            FloatingTextSpawner.Instance.Spawn(pos, text, type);
        }

        private void OnDead()
        {
            Debug.Log("[PlayerStats] Player died.");
            // TODO: 게임 오버 처리
        }
    }
}
