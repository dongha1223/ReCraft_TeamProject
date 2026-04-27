using System;
using System.Collections.Generic;

namespace _2D_Roguelike
{
    /// <summary>
    /// 각인의 단계 하나를 정의한다.
    /// 예: requiredCount = 4 → 해당 각인이 4개 이상 장착되면 effects 발동
    /// </summary>
    [Serializable]
    public class InscriptionTierDefinition
    {
        public int                    requiredCount;
        public List<EffectDefinition> effects;
    }
}
