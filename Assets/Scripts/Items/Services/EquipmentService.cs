using System;
using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 장착 중인 아이템 목록을 관리한다.
    /// 장착/해제 시 이벤트를 발행하며, LoadoutEffectCoordinator가 이를 구독해
    /// 효과를 자동으로 재계산한다.
    /// </summary>
    public class EquipmentService
    {
        private readonly List<ItemInstance> _equippedItems = new();

        public IReadOnlyList<ItemInstance> EquippedItems => _equippedItems;

        public event Action<ItemInstance> OnItemEquipped;
        public event Action<ItemInstance> OnItemUnequipped;

        /// <summary>
        /// 장착 가능 여부 검사.
        /// 현재는 중복 장착만 막는다. 슬롯 제한 등은 추후 이곳에서 확장.
        /// </summary>
        public bool CanEquip(ItemInstance item)
        {
            return item != null && !_equippedItems.Contains(item);
        }

        public void Equip(ItemInstance item)
        {
            if (!CanEquip(item)) return;
            _equippedItems.Add(item);
            OnItemEquipped?.Invoke(item);
        }

        public void Unequip(ItemInstance item)
        {
            if (!_equippedItems.Remove(item)) return;
            OnItemUnequipped?.Invoke(item);
        }
    }
}
