using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 장착 아이템 기준으로 합산된 각인 개수를 보관하는 순수 상태 객체.
    /// InscriptionService가 장착 변경 시마다 이 상태를 재계산한다.
    /// </summary>
    public class InscriptionState
    {
        private readonly Dictionary<string, int> _counts = new();

        public IReadOnlyDictionary<string, int> Counts => _counts;

        public int GetCount(string inscriptionId)
        {
            return _counts.TryGetValue(inscriptionId, out var count) ? count : 0;
        }

        public void SetCount(string inscriptionId, int count)
        {
            _counts[inscriptionId] = count;
        }

        public void Clear()
        {
            _counts.Clear();
        }
    }
}
