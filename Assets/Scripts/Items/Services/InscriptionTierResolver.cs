using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 각인 수에서 활성화된 단계 목록을 계산한다.
    /// 예: tiers가 [2개, 4개, 6개]이고 현재 카운트가 4면 → [2개 단계, 4개 단계] 반환
    /// </summary>
    public class InscriptionTierResolver
    {
        public List<InscriptionTierDefinition> GetActiveTiers(
            InscriptionDefinition inscription,
            int currentCount)
        {
            var result = new List<InscriptionTierDefinition>();

            if (inscription.tiers == null) return result;

            foreach (var tier in inscription.tiers)
            {
                if (currentCount >= tier.requiredCount)
                    result.Add(tier);
            }

            return result;
        }
    }
}
