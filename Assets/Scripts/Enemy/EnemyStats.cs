using System.Collections;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyStats : MonoBehaviour, IDamageable
    {
        [Header("스탯")]
        [SerializeField] private float _maxHp = 70f;

        [Header("데미지 텍스트")]
        [SerializeField] private Transform _damageSpawnPos;  // 적 머리 위 빈 Transform (없으면 중심 + offset 사용)

        private float                 _currentHp;
        private bool                  _isDead;
        private Animator              _animator;
        private EnemyController       _controller;
        private EnemyRangedController _rangedController;
        private DamageFlash           _damageFlash;
        private KnockbackReceiver     _knockback;

        private static readonly int AnimDie = Animator.StringToHash("Die");
        private static readonly int AnimHit = Animator.StringToHash("Hit");

        public bool IsDead        => _isDead;
        public bool IsInvincible  => false;

        private void Awake()
        {
            _currentHp        = _maxHp;
            _animator         = GetComponent<Animator>();
            _controller       = GetComponent<EnemyController>();
            _rangedController = GetComponent<EnemyRangedController>();
            _damageFlash      = GetComponent<DamageFlash>();
            _knockback        = GetComponent<KnockbackReceiver>();
        }

        // ── 안전한 Animator 트리거 ────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────
        public void TakeDamage(HitInfo info)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - info.Damage);
            Debug.Log($"[EnemyStats] {name} HP: {_currentHp}/{_maxHp}  (-{info.Damage})");

            SpawnDamageText(info.Damage);

            if (info.KnockbackForce > 0f)
                _knockback?.ApplyKnockback(info.SourcePosition, info.KnockbackForce);

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

        private void SpawnDamageText(float amount)
        {
            if (FloatingTextSpawner.Instance == null) return;
            var pos = _damageSpawnPos != null
                ? _damageSpawnPos.position
                : transform.position + new Vector3(0f, 0.8f, 0f);
            FloatingTextSpawner.Instance.Spawn(pos, Mathf.RoundToInt(amount).ToString(), FloatingTextType.Damage);
        }

        private void OnDead()
        {
            Debug.Log($"[EnemyStats] {name} 사망.");
            if (_controller != null)       _controller.enabled = false;
            if (_rangedController != null) _rangedController.enabled = false;
            SafeSetTrigger(AnimDie);

            // 스테이지 매니저에 적 사망 통보
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
            if (_controller != null)       _controller.enabled = true;
            if (_rangedController != null) _rangedController.enabled = true;
        }

        public float getMaxHP()
        {
            return _maxHp;
        }

        public float getCurrnetHP()
        {
            return _currentHp;
        }
    }
}
