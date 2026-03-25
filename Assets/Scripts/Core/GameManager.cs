using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 게임 매니저
    /// 수정 사항:
    ///   - IgnoreLayerCollision 레이어 이름 유효성 검사 추가
    ///     (레이어 미존재 시 -1 반환 → 잘못된 충돌 설정 방지)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            SetupLayerCollisions();
        }

        private static void SetupLayerCollisions()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer  = LayerMask.NameToLayer("Enemy");

            // 레이어가 존재하지 않으면 -1 반환 → 건너뜀
            if (playerLayer < 0)
            {
                Debug.LogWarning("[GameManager] 'Player' 레이어를 찾을 수 없습니다. Physics 설정 확인 필요.");
                return;
            }
            if (enemyLayer < 0)
            {
                Debug.LogWarning("[GameManager] 'Enemy' 레이어를 찾을 수 없습니다. Physics 설정 확인 필요.");
                return;
            }

            // 플레이어↔적 물리 충돌 무시 (밀침 방지)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }
    }
}
