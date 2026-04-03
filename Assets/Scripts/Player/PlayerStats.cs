using UnityEngine;

namespace _2D_Roguelike
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float     _maxHp           = 100f;
        [SerializeField] private Transform _damageSpawnPos;  // 플레이어 머리 위 빈 Transform (없으면 중심 + offset 사용)

        private float _currentHp;

        public float CurrentHp => _currentHp;
        public float MaxHp     => _maxHp;
        public bool  IsDead    => _currentHp <= 0f;

        private void Awake()
        {
            _currentHp = _maxHp;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            Debug.Log($"[PlayerStats] HP: {_currentHp}/{_maxHp}");

            SpawnFloatingText(amount, FloatingTextType.Damage);

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
