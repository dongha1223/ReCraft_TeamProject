using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어가 보유한 아이템 목록을 관리한다.
    /// 획득/제거만 담당하며, 장착 상태는 EquipmentService가 별도로 관리한다.
    /// </summary>
    public class InventoryService
    {
        public const int MaxCapacity = 9;

        private readonly List<ItemInstance> _items = new();

        public IReadOnlyList<ItemInstance> Items  => _items;
        public bool                        IsFull => _items.Count >= MaxCapacity;

        public void Add(ItemInstance item)
        {
            if (item == null || _items.Contains(item) || IsFull) return;
            _items.Add(item);
        }

        public void Remove(ItemInstance item)
        {
            _items.Remove(item);
        }

        public bool Contains(string instanceId)
        {
            return _items.Exists(i => i.InstanceId == instanceId);
        }

        public void Clear() => _items.Clear();
    }
}
