using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float     _damage        = 10f;
        [SerializeField] private float     _attackCooldown = 0.5f;
        [SerializeField] private Vector2   _hitboxSize    = new Vector2(1.2f, 0.8f);
        [SerializeField] private Vector2   _hitboxOffset  = new Vector2(0.7f, 0f);
        [SerializeField] private LayerMask _enemyLayer;

        [Header("넉백")]
        [SerializeField] private float _knockbackForce = 5f;

        [Header("기본 공격 고유 상태이상 (아이템 무관 고정 효과)")]
        [SerializeField] private StatusEffectSpec[] _innateStatusEffects;

        private Animator             _animator;
        private PlayerStatController _statController;
        private OnHitStatusRegistry  _onHitRegistry;

        private bool _isAttacking;
        private bool _canAttack = true;

        private static readonly int AnimAttack = Animator.StringToHash("Attack");

        private void Awake()
        {
            _animator       = GetComponent<Animator>();
            _statController = GetComponent<PlayerStatController>();
            _onHitRegistry  = GetComponent<OnHitStatusRegistry>();
        }

        private void Start()
        {
            // Inspector 수치를 기본값으로 StatService에 등록
            _statController?.StatService.SetBaseValue(StatType.AttackPower, _damage);
        }

        private void Update()
        {
            if (UIState.IsBlockingInput || _isAttacking) return;

            if (KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Attack) && _canAttack)
                StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            _isAttacking = true;
            _canAttack   = false;

            _animator?.SetTrigger(AnimAttack);

            // 공격 모션 중간(0.2초 후)에 히트박스 판정
            yield return new WaitForSeconds(0.2f);
            ApplyHitbox();

            yield return new WaitForSeconds(_attackCooldown - 0.2f);

            _isAttacking = false;
            _canAttack   = true;
        }

        private void ApplyHitbox()
        {
            float dir      = transform.localScale.x < 0f ? -1f : 1f;
            Vector2 center = (Vector2)transform.position + new Vector2(_hitboxOffset.x * dir, _hitboxOffset.y);

            float finalDamage = _statController != null
                ? _statController.StatService.GetFinalValue(StatType.AttackPower)
                : _damage;

            StatusEffectSpec[] statusEffects = MergeSpecs(
                _innateStatusEffects,
                _onHitRegistry?.GetSpecsFor(OnHitTarget.BasicAttack));

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, _hitboxSize, 0f, _enemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;

                damageable.TakeDamage(new HitInfo
                {
                    Damage         = finalDamage,
                    SourcePosition = transform.position,
                    KnockbackForce = _knockbackForce,
                    StatusEffects  = statusEffects
                });
            }
        }

        /// <summary>인스펙터 고정 스펙과 레지스트리(아이템·각인) 스펙을 하나의 배열로 합산</summary>
        private static StatusEffectSpec[] MergeSpecs(StatusEffectSpec[] innate, StatusEffectSpec[] fromRegistry)
        {
            bool hasInnate   = innate       != null && innate.Length       > 0;
            bool hasRegistry = fromRegistry != null && fromRegistry.Length > 0;

            if (!hasInnate && !hasRegistry) return null;
            if (!hasInnate)   return fromRegistry;
            if (!hasRegistry) return innate;

            var merged = new StatusEffectSpec[innate.Length + fromRegistry.Length];
            innate.CopyTo(merged, 0);
            fromRegistry.CopyTo(merged, innate.Length);
            return merged;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            float dir    = transform.localScale.x < 0f ? -1f : 1f;
            Vector2 center = (Vector2)transform.position + new Vector2(_hitboxOffset.x * dir, _hitboxOffset.y);
            Gizmos.DrawWireCube(center, _hitboxSize);
        }
    }
}
