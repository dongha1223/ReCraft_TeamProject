using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 미니맵에 표시될 아이콘 컴포넌트입니다.
    /// 이 스크립트를 붙인 오브젝트를 "Minimap" 레이어로 설정합니다.
    /// SpriteRenderer와 스프라이트 에셋은 프리팹에 미리 구성해야 합니다.
    ///   - 적: 빨간 원 스프라이트 (Assets/Sprites/Minimap/)
    ///   - 플레이어, 지형: 사용자가 직접 추가
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        private void Awake()
        {
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer == -1)
            {
                // "Minimap" 레이어가 Tags & Layers에 등록되지 않은 경우 경고
                Debug.LogWarning("[MinimapIcon] 'Minimap' 레이어가 존재하지 않습니다. Tags & Layers에 추가해 주세요.");
                return;
            }

            gameObject.layer = minimapLayer;
        }
    }
}
