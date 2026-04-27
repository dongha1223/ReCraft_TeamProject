using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 장착 아이템을 기준으로 각인 누적 수를 관리한다.
    /// 장착 변경이 생길 때마다 RebuildFromEquipped를 호출해 전체를 재계산한다.
    /// </summary>
    public class InscriptionService
    {
        private readonly InscriptionState _state = new();

        public InscriptionState State => _state;

        public void RebuildFromEquipped(IReadOnlyList<ItemInstance> equippedItems)
        {
            _state.Clear();

            foreach (var item in equippedItems)
            {
                if (item.Definition.inscriptions == null) continue;

                foreach (var entry in item.Definition.inscriptions)
                {
                    if (entry.inscription == null) continue;

                    var id      = entry.inscription.inscriptionId;
                    var current = _state.GetCount(id);
                    _state.SetCount(id, current + entry.amount);
                }
            }
        }

        public int GetCount(InscriptionDefinition inscription)
        {
            return _state.GetCount(inscription.inscriptionId);
        }
    }
}
