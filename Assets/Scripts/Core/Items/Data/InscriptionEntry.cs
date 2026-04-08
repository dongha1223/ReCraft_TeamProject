using System;

namespace _2D_Roguelike
{
    /// <summary>
    /// 아이템이 제공하는 각인 정보 한 건.
    /// 대부분 amount = 1이지만, 특수 아이템은 2 이상도 가능하다.
    /// </summary>
    [Serializable]
    public struct InscriptionEntry
    {
        public InscriptionDefinition inscription;
        public int                   amount;
    }
}
