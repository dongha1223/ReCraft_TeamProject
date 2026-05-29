using System;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 보스 파츠(몸통/머리/팔 등)에 붙는 독립 HP 컴포넌트.
    /// HP가 0이 되면 파츠 오브젝트를 비활성화하고 OnPartDestroyed를 발행한다.
    /// 데미지는 이 오브젝트에서만 처리하며, 상태이상은 루트의 StatusController로 포워딩한다.
    /// </summary>
    public class BossPartHP : MonoBehaviour, IDamageable
    {
        [Header("데이터 SO")]
        [SerializeField] private BossPartHPDataSO _data;

        [SerializeField] private float _maxHp = 1000f;

        [Tooltip("이 파츠에 가해지는 데미지 배율 (머리=약점이면 1.5 등)")]
        [SerializeField] private float _damageMultiplier = 1f;

        [Tooltip("루트 StatusController — 상태이상 포워딩용. Inspector에서 연결.")]
        [SerializeField] private StatusController _rootStatusController;

        [SerializeField] private AudioClip[] _hitClips;

        public event Action OnPartDestroyed;

        private float            _currentHp;
        private bool             _isDead;
        private DamageFlash      _damageFlash;
        private HitEffectSpawner _hitEffectSpawner;

        public bool IsDead       => _isDead;
        public bool IsInvincible => _isDead;

        private void Awake()
        {
            if (_data != null)
            {
                _maxHp            = _data.MaxHp;
                _damageMultiplier = _data.DamageMultiplier;
                _hitClips         = _data.HitClips;
            }
            _currentHp   = _maxHp;
            _damageFlash = GetComponent<DamageFlash>();
        }

        private void Start()
        {
            _hitEffectSpawner = HitEffectSpawner.Instance;
        }

        public void TakeDamage(HitInfo info)
        {
            if (_isDead) return;

            float damage = info.Damage * _damageMultiplier;
            _currentHp = Mathf.Max(0f, _currentHp - damage);

            FloatingTextSpawner.Instance?.Spawn(
                transform.position + Vector3.up * 0.5f,
                Mathf.RoundToInt(damage).ToString(),
                FloatingTextType.Damage);

            _hitEffectSpawner?.Spawn(transform.position);
            _damageFlash?.CallDamageFlash();

            HitSoundAggregator.Instance?.RegisterHit(
                info.AttackId, transform.position, _hitClips, null);

            // 상태이상 루트로 포워딩
            if (_rootStatusController != null)
            {
                _rootStatusController.OnHitReceived(info);
                if (info.StatusEffects != null)
                    foreach (var spec in info.StatusEffects)
                        _rootStatusController.ApplyStatus(spec);
            }

            if (_currentHp <= 0f)
            {
                _isDead = true;
                OnPartDestroyed?.Invoke();
                gameObject.SetActive(false);
            }
        }

        /// <summary>스테이지 재진입 시 파츠를 초기 상태로 복원한다.</summary>
        public void Reset()
        {
            _isDead    = false;
            _currentHp = _maxHp;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public float GetCurrentHP() => _currentHp;
        public float GetMaxHP()     => _maxHp;
    }
}
