using UnityEngine;

namespace _2D_Roguelike
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float _maxHp = 100f;

        private float _currentHp;

        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public bool IsDead => _currentHp <= 0f;

        private void Awake()
        {
            _currentHp = _maxHp;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHp = Mathf.Max(0f, _currentHp - amount);
            Debug.Log($"[PlayerStats] HP: {_currentHp}/{_maxHp}");

            if (IsDead)
                OnDead();
        }

        private void OnDead()
        {
            Debug.Log("[PlayerStats] Player died.");
            // TODO: 게임 오버 처리
        }
    }
}
