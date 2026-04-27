using System;

namespace _2D_Roguelike
{
    /// <summary>
    /// 플레이어가 실제로 보유/장착 중인 아이템 인스턴스.
    /// ScriptableObject(ItemDefinition)는 원본 데이터이고,
    /// 이 클래스가 런타임 상태(강화 수치, 잠금 등)를 담당한다.
    /// </summary>
    public class ItemInstance
    {
        /// <summary>인스턴스 고유 ID. 저장/효과 추적에 사용</summary>
        public string         InstanceId { get; }
        public ItemDefinition Definition { get; }

        public ItemInstance(ItemDefinition definition)
        {
            InstanceId = Guid.NewGuid().ToString();
            Definition = definition;
        }
    }
}
