using UnityEngine;

namespace _2D_Roguelike
{
    public class NPCHealEffect : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 50f;

        private PlayerStats _playerStats;

        public void Execute()
        {
            if (_playerStats == null)
                _playerStats = FindFirstObjectByType<PlayerStats>();

            _playerStats?.Heal(_healAmount);
        }
    }
}
