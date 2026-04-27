using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class PlayerAttack : MonoBehaviour
    {
        [System.Serializable]
        private struct ComboStep
        {
            public float    damage;
            public Vector2  hitboxSize;
            public Vector2  hitboxOffset;
            public float    hitTiming;      // 모션 시작 후 히트박스 판정까지 대기 시간
            public float    duration;       // 이 단계 전체 지속 시간 (다음 입력 윈도우 포함)
            public float    impulseForce;   // 공격 시작 시 앞으로 가해지는 단발 힘
        }

        [Header("콤보 설정")]
        [SerializeField] private ComboStep[] _comboSteps = new ComboStep[]
        {
            new ComboStep { damage = 10f, hitboxSize = new Vector2(1.2f, 0.8f), hitboxOffset = new Vector2(0.7f, 0f), hitTiming = 0.15f, duration = 0.40f, impulseForce = 3f },
            new ComboStep { damage = 12f, hitboxSize = new Vector2(1.3f, 0.8f), hitboxOffset = new Vector2(0.8f, 0f), hitTiming = 0.15f, duration = 0.40f, impulseForce = 3f },
            new ComboStep { damage = 18f, hitboxSize = new Vector2(1.5f, 1.0f), hitboxOffset = new Vector2(0.8f, 0f), hitTiming = 0.20f, duration = 0.55f, impulseForce = 5f },
        };

        [Header("콤보 입력 윈도우")]
        [SerializeField] private float _comboWindowTime = 0.25f; // 모션 종료 후 다음 입력 수락 시간

        [Header("넉백")]
        [SerializeField] private float _knockbackForce = 5f;

        [Header("기본 공격 고유 상태이상 (아이템 무관 고정 효과)")]
        [SerializeField] private StatusEffectSpec[] _innateStatusEffects;

        [SerializeField] private LayerMask _enemyLayer;

        private Rigidbody2D          _rb;
        private Animator             _animator;
        private PlayerStatController _statController;
        private OnHitStatusRegistry  _onHitRegistry;
        private FormManager          _formManager;

        private int  _comboIndex;       // 다음에 실행할 콤보 단계
        private bool _isAttacking;
        private bool _nextInputQueued;  // 콤보 윈도우 내 입력 감지

        public bool IsAttacking => _isAttacking;

        private static readonly int AnimComboStage = Animator.StringToHash("ComboStage");

        private void Awake()
        {
            _rb             = GetComponent<Rigidbody2D>();
            _animator       = GetComponent<Animator>();
            _statController = GetComponent<PlayerStatController>();
            _onHitRegistry  = GetComponent<OnHitStatusRegistry>();
            _formManager    = GetComponent<FormManager>();
        }

        private void Start()
        {
            _statController?.StatService.SetBaseValue(StatType.AttackPower, _comboSteps[0].damage);
        }

        private void Update()
        {
            if (UIState.IsBlockingInput) return;

            if (KeyBindingService.WasPressedThisFrame(KeyBindingService.Action.Attack))
            {
                if (!_isAttacking)
                    StartCoroutine(ComboCoroutine());
                else
                    _nextInputQueued = true; // 콤보 윈도우 대기 중 입력 예약
            }
        }

        private IEnumerator ComboCoroutine()
        {
            _isAttacking = true;
            _comboIndex  = 0;

            while (_comboIndex < _comboSteps.Length)
            {
                ComboStep step = _comboSteps[_comboIndex];
                _nextInputQueued = false;

                // 방향키 입력 중일 때만 단발 전진 임펄스 적용
                bool movingLeft  = KeyBindingService.IsPressed(KeyBindingService.Action.MoveLeft);
                bool movingRight = KeyBindingService.IsPressed(KeyBindingService.Action.MoveRight);
                if (_rb != null && step.impulseForce > 0f && (movingLeft || movingRight))
                {
                    float dir = transform.localScale.x < 0f ? -1f : 1f;
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    _rb.AddForce(new Vector2(dir * step.impulseForce, 0f), ForceMode2D.Impulse);
                }

                // ComboStage: 1=Attack, 2=Attack2, 3=Attack3
                _animator?.SetInteger(AnimComboStage, _comboIndex + 1);

                // 히트박스 판정 타이밍 대기
                yield return new WaitForSeconds(step.hitTiming);
                ApplyHitbox(step);

                // 나머지 모션 재생 시간 대기
                float remaining = step.duration - step.hitTiming;
                yield return new WaitForSeconds(remaining);

                _comboIndex++;

                // 마지막 콤보면 바로 종료
                if (_comboIndex >= _comboSteps.Length)
                {
                    
                    break;
                }
                

                // 콤보 윈도우: 이미 입력이 예약돼 있거나, 시간 내 새 입력 수락
                float windowElapsed = 0f;
                while (windowElapsed < _comboWindowTime)
                {
                    if (_nextInputQueued)
                        break;

                    windowElapsed += Time.deltaTime;
                    yield return null;
                }

                // 윈도우 내 입력 없으면 콤보 종료
                if (!_nextInputQueued)
                {
                    
                    break;
                }
            }

            _isAttacking     = false;
            _nextInputQueued = false;
            _comboIndex      = 0;
            _animator?.SetInteger(AnimComboStage, 0); // 콤보 종료 → 비공격 상태
        }

        private void ApplyHitbox(ComboStep step)
        {
            float dir      = transform.localScale.x < 0f ? -1f : 1f;
            Vector2 center = (Vector2)transform.position + new Vector2(step.hitboxOffset.x * dir, step.hitboxOffset.y);

            float baseDamage  = step.damage;
            float finalDamage = _statController != null
                ? _statController.StatService.GetFinalValue(StatType.AttackPower) * (baseDamage / _comboSteps[0].damage)
                : baseDamage;

            StatusEffectSpec[] statusEffects = MergeSpecs(
                _innateStatusEffects,
                _onHitRegistry?.GetSpecsFor(OnHitTarget.BasicAttack));

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, step.hitboxSize, 0f, _enemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;

                damageable.TakeDamage(new HitInfo
                {
                    Damage         = finalDamage,
                    DamageType     = _formManager?.Current?.PrimaryDamageType ?? DamageType.Physical,
                    SourcePosition = transform.position,
                    KnockbackForce = _knockbackForce,
                    StatusEffects  = statusEffects
                });
            }
        }

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
            if (_comboSteps == null) return;
            Gizmos.color = Color.red;
            float dir = transform.localScale.x < 0f ? -1f : 1f;
            foreach (var step in _comboSteps)
            {
                Vector2 center = (Vector2)transform.position + new Vector2(step.hitboxOffset.x * dir, step.hitboxOffset.y);
                Gizmos.DrawWireCube(center, step.hitboxSize);
            }
        }
    }
}
