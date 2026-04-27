using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 화상 상태이상.
    /// 빠른 틱 주기로 데미지를 준다.
    /// 추후 치명타 시스템 추가 시 Tick()을 override해 crit 롤을 적용한다.
    /// </summary>
    public class BurnEffect : DotEffectBase
    {
        private static readonly Color TintColor = new Color(1f, 0.25f, 0.1f);

        private DamageFlash _damageFlash;

        public BurnEffect(StatusController owner, StatusEffectSpec spec)
            : base(owner, StatusEffectType.Burn, spec) { }

        public override void OnApply()
        {
            base.OnApply();
            _damageFlash = Owner.GetComponent<DamageFlash>();
            _damageFlash?.SetStatusTint(TintColor);
        }

        public override void OnRemove()
        {
            _damageFlash?.ClearStatusTint();
        }
    }
}
