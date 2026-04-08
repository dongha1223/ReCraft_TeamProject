using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyStats : MonoBehaviour, IDamageable, IDotReceiver
    {
        [Header("스탯")]
        [SerializeField] private float _maxHp = 70f;

        [Header("데미지 텍스트")]
        [SerializeField] private Transform _damageSpawnPos;  // 적 머리 위 빈 Transform (없으면 중심 + offset 사용)

        private float             _currentHp;
        private bool              _isDead;
        private Animator          _animator;
        private EnemyBrainBase    _brain;
        private DamageFlash       _damageFlash;
        private KnockbackReceiver _knockback;
        private StatusController  _statusController;

        private static readonly int AnimDie = Animator.StringToHash("Die");
        private static readonly int AnimHit = Animator.StringToHash("Hit");

        public bool IsDead       => _isDead;
        public bool IsInvincible => false;

        private void Awake()
        {
            _currentHp        = _maxHp;
            _animator         = GetComponent<Animator>();
            _brain            = GetComponent<EnemyBrainBase>();
            _damageFlash      = GetComponent<DamageFlash>();
            _knockback        = GetComponent<KnockbackReceiver>();
            _statusController = GetComponent<StatusController>();
        }

        /// <summary>파라미터가 존재할 때만 SetTrigger — 없으면 조용히 무시</summary>
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

        public void TakeDamage(HitInfo info)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - info.Damage);
            Debug.Log($"[EnemyStats] {name} HP: {_currentHp}/{_maxHp}  (-{info.Damage})");

            SpawnDamageText(info.Damage);

            if (info.KnockbackForce > 0f)
                _knockback?.ApplyKnockback(info.SourcePosition, info.KnockbackForce);

            // 피격 이벤트 먼저 전파 (기존 빙결 해제 등 — 새 상태이상 적용보다 반드시 선행)
            _statusController?.OnHitReceived(info);

            // 상태이상 적용 (OnHitReceived 이후여야 새 빙결이 즉시 해제되지 않음)
            if (info.StatusEffects != null && _statusController != null)
            {
                foreach (var spec in info.StatusEffects)
                    _statusController.ApplyStatus(spec);
            }

            if (_currentHp <= 0f)
            {
                _isDead = true;
                OnDead();
            }
            else
            {
                _damageFlash?.CallDamageFlash();
                SafeSetTrigger(AnimHit);
            }
        }

        /// <summary>
        /// DoT 전용 데미지 처리.
        /// 넉백·무적·피격 애니메이션 없이 체력만 깎는다.
        /// </summary>
        public void TakeDotDamage(float amount)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            SpawnDamageText(amount, FloatingTextType.StatusEffect);

            if (_currentHp <= 0f)
            {
                _isDead = true;
                OnDead();
            }
        }

        private void SpawnDamageText(float amount, FloatingTextType type = FloatingTextType.Damage)
        {
            if (FloatingTextSpawner.Instance == null) return;
            var pos = _damageSpawnPos != null
                ? _damageSpawnPos.position
                : transform.position + new Vector3(0f, 0.8f, 0f);
            FloatingTextSpawner.Instance.Spawn(pos, Mathf.RoundToInt(amount).ToString(), type);
        }

        private void OnDead()
        {
            Debug.Log($"[EnemyStats] {name} 사망.");
            if (_brain != null) _brain.enabled = false;
            SafeSetTrigger(AnimDie);

            StageManager.Instance?.OnEnemyDied();
            StartCoroutine(ReturnToPoolAfterDelay(1.5f));
        }

        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (EnemyPool.Instance != null)
                EnemyPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }

        public void ResetStats()
        {
            _isDead    = false;
            _currentHp = _maxHp;
            _knockback?.ResetKnockback();
            _statusController?.ClearAll();
            if (_brain != null) _brain.enabled = true;
        }

        public float getMaxHP()     => _maxHp;
        public float getCurrnetHP() => _currentHp;
    }
}
