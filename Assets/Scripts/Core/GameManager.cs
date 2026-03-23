using UnityEngine;

namespace _2D_Roguelike
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            // 플레이어와 적 레이어 간 물리 충돌 완전 무시
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Enemy"),
                true
            );
        }
    }
}
