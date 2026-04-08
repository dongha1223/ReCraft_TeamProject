namespace _2D_Roguelike
{
    /// <summary>
    /// 출혈 상태이상.
    /// 중간 틱 주기로 데미지를 준다. 치명타 적용 없음.
    /// </summary>
    public class BleedEffect : DotEffectBase
    {
        public BleedEffect(StatusController owner, StatusEffectSpec spec)
            : base(owner, StatusEffectType.Bleed, spec) { }
    }
}
