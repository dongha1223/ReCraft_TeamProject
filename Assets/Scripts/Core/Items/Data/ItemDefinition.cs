using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 아이템 원본 데이터. 런타임 상태를 갖지 않는 순수 정의 에셋.
    /// 새 아이템을 추가할 때는 이 에셋을 Create → 값 설정만 하면 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "2D Roguelike/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("기본 정보")]
        public string       itemId;
        public string       displayName;
        public Sprite       icon;
        public ItemRarity   rarity;
        public ItemCategory category;

        [TextArea]
        public string description;

        [Header("각인")]
        public List<InscriptionEntry> inscriptions;

        [Header("효과")]

        [Header("드랍 설정")]
        [Tooltip("가중치가 높을수록 드랍 확률 상승")]
        public float baseDropWeight = 1f;

        [Tooltip("드랍되는 맵 테마. 비어있으면 모든 테마에서 드랍 가능 (Common 아이템 등)")]
        public MapTheme[] dropThemes;
        public List<EffectDefinition> effects;
    }
}
