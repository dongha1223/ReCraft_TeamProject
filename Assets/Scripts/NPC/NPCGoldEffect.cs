using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// NPC 선택지 "예" 응답 시 골드를 지급하는 컴포넌트.
    /// NPCController._onYesChosen 이벤트에 Execute()를 연결해 사용.
    /// </summary>
    public class NPCGoldEffect : MonoBehaviour
    {
        [SerializeField] private int _goldAmount = 50;

        public void Execute()
        {
            GoldManager.Instance?.AddGold(_goldAmount);
        }
    }
}
