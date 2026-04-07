namespace _2D_Roguelike
{
    /// <summary>
    /// 틱 주기마다 데미지를 주는 DoT(Damage over Time) 상태이상의 공통 기반.
    /// Burn / Bleed / Poison 이 상속한다.
    /// TakeDotDamage 를 통해 넉백·무적·피격 애니메이션 없이 체력만 깎는다.
    /// </summary>
    public abstract class DotEffectBase : StatusEffectBase
    {
        protected float       TickDamage;
        protected float       TickInterval;
        private   float       _tickTimer;
        private   IDotReceiver _dotReceiver;

        protected DotEffectBase(StatusController owner, StatusEffectType type, StatusEffectSpec spec)
            : base(owner, type, spec.duration)
        {
            TickDamage   = spec.tickDamage;
            TickInterval = spec.tickInterval;
        }

        public override void OnApply()
        {
            _dotReceiver = Owner.GetComponent<IDotReceiver>();
            _tickTimer   = 0f;
        }

        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            if (_dotReceiver == null || _dotReceiver.IsDead) return;

            _tickTimer += dt;
            if (_tickTimer >= TickInterval)
            {
                _tickTimer -= TickInterval;
                Tick();
            }
        }

        /// <summary>틱 발동 시 호출 — 서브클래스에서 override 가능 (치명타 등 확장용)</summary>
        protected virtual void Tick()
        {
            _dotReceiver.TakeDotDamage(TickDamage);
        }
    }
}
