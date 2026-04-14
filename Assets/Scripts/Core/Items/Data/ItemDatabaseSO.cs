using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 전체 아이템 원본 데이터 목록. DropSystem이 참조하는 마스터 DB.
    /// </summary>
    [CreateAssetMenu(menuName = "2D Roguelike/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabaseSO : ScriptableObject
    {
        [Tooltip("게임 내 모든 아이템 정의 에셋을 여기에 등록")]
        public ItemDefinition[] items;
    }
}
