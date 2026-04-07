using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 중독 상태이상.
    /// 느린 틱 주기로 데미지를 준다. 치명타 적용 없음.
    /// </summary>
    public class PoisonEffect : DotEffectBase
    {
        private static readonly Color TintColor = new Color(0.2f, 1f, 0.3f);

        private DamageFlash _damageFlash;

        public PoisonEffect(StatusController owner, StatusEffectSpec spec)
            : base(owner, StatusEffectType.Poison, spec) { }

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
