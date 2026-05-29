using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    public class EnemyStats : MonoBehaviour, IDamageable, IDotReceiver
    {
        /// <summary>데미지를 받을 때마다 발행 (currentHp, maxHp).</summary>
        public event Action<float, float> OnHPChanged;

        /// <summary>사망 확정 시 발행. BossMainController 등이 구독해 패턴 루프를 종료한다.</summary>
        public event Action OnDeadEvent;

        [Header("데이터 SO")]
        [SerializeField] private EnemyDataSO _data;

        [Header("스탯")]
        [SerializeField] private float _maxHp = 70f;

        [Header("데미지 텍스트")]
        [SerializeField] private Transform _damageSpawnPos;  // 적 머리 위 빈 Transform (없으면 중심 + offset 사용)

        [Header("피격 사운드")]
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _heavyHitClips; // 다중 적중 시 사용 (비워두면 hitClips로 대체)

        private float             _currentHp;
        private bool              _isDead;
        private Animator          _animator;
        private EnemyBrainBase    _brain;
        private DamageFlash       _damageFlash;
        private HitEffectSpawner  _hitEffectSpawner;
        private KnockbackReceiver _knockback;
        private StatusController  _statusController;
        private TagTokenBank      _tagTokenBank;

        private static readonly int AnimDie = Animator.StringToHash("Die");
        private static readonly int AnimHit = Animator.StringToHash("Hit");

        private HashSet<int> _validTriggerHashes;

        public bool IsDead       => _isDead;
        public bool IsInvincible => false;

        private void Awake()
        {
            if (_data != null)
            {
                _maxHp         = _data.MaxHp;
                _hitClips      = _data.HitClips;
                _heavyHitClips = _data.HeavyHitClips;
            }
            _currentHp        = _maxHp;
            _animator         = GetComponent<Animator>();
            _brain            = GetComponent<EnemyBrainBase>();
            _damageFlash      = GetComponent<DamageFlash>();
            _knockback        = GetComponent<KnockbackReceiver>();
            _statusController = GetComponent<StatusController>();
            CacheTriggerHashes();
        }

        private void CacheTriggerHashes()
        {
            _validTriggerHashes = new HashSet<int>();
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger)
                    _validTriggerHashes.Add(p.nameHash);
        }

        private void Start()
        {
            // 풀링 시 재사용되므로 Start에서 캐싱 (씬 로드 후 플레이어가 생성된 뒤)
            _tagTokenBank     = FindFirstObjectByType<TagTokenBank>();
            _hitEffectSpawner = HitEffectSpawner.Instance;
        }

        /// <summary>파라미터가 존재할 때만 SetTrigger — Awake에 캐싱된 HashSet으로 O(1) 체크</summary>
        private void SafeSetTrigger(int hash)
        {
            if (_animator == null) return;
            if (_validTriggerHashes.Contains(hash))
                _animator.SetTrigger(hash);
        }

        public void TakeDamage(HitInfo info)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - info.Damage);

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

            OnHPChanged?.Invoke(_currentHp, _maxHp);

            if (_currentHp <= 0f)
            {
                _isDead = true;
                OnDead();
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

        /// <summary>
        /// DoT 전용 데미지 처리.
        /// 넉백·무적·피격 애니메이션 없이 체력만 깎는다.
        /// </summary>
        public void TakeDotDamage(float amount)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            SpawnDamageText(amount, FloatingTextType.StatusEffect);

            OnHPChanged?.Invoke(_currentHp, _maxHp);

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
            if (_brain != null) _brain.enabled = false;
            OnDeadEvent?.Invoke();
            SafeSetTrigger(AnimDie);

            // 처치 시 플레이어 태그 토큰 게이지 획득
            _tagTokenBank?.Gain(_tagTokenBank.GainPerKill);

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
