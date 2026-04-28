using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// NPC 선택지 "예" 응답 시 플레이어 체력을 회복시키는 컴포넌트.
    /// NPCController._onYesChosen 이벤트에 Execute()를 연결해 사용.
    /// </summary>
    public class NPCHealEffect : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 50f;

        private PlayerStats _playerStats;

        public void Execute()
        {
            if (_playerStats == null)
                _playerStats = FindObjectOfType<PlayerStats>();

            _playerStats?.Heal(_healAmount);
        }
    }
}
