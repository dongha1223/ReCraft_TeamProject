using UnityEngine;

namespace _2D_Roguelike
{
    public class NPCGoldEffect : MonoBehaviour
    {
        [SerializeField] private int _goldAmount = 50;

        public void Execute()
        {
            GoldManager.Instance?.AddGold(_goldAmount);
        }
    }
}
