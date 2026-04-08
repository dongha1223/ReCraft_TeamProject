namespace _2D_Roguelike
{
    /// <summary>
    /// 모든 상태이상 런타임 인스턴스의 공통 기반.
    /// OnApply → OnUpdate(매 프레임) → OnRemove 생명주기를 가진다.
    /// </summary>
    public abstract class StatusEffectBase
    {
        protected StatusController Owner;
        protected float            Duration;
        protected float            Elapsed;

        public StatusEffectType EffectType { get; }
        public bool             IsFinished  => Elapsed >= Duration;

        protected StatusEffectBase(StatusController owner, StatusEffectType type, float duration)
        {
            Owner      = owner;
            EffectType = type;
            Duration   = duration;
        }

        /// <summary>처음 적용될 때 1회 호출</summary>
        public virtual void OnApply() { }

        /// <summary>같은 타입이 다시 걸릴 때 호출 — 기본 동작: 지속시간 갱신</summary>
        public virtual void OnRefresh(StatusEffectSpec newSpec)
        {
            Elapsed  = 0f;
            Duration = newSpec.duration;
        }

        /// <summary>매 프레임 StatusController.Update()에서 호출</summary>
        public virtual void OnUpdate(float dt)
        {
            Elapsed += dt;
        }

        /// <summary>만료 또는 강제 해제될 때 1회 호출</summary>
        public virtual void OnRemove() { }

        /// <summary>
        /// 피격이 발생했을 때 호출 (빙결 해제 등 피격 반응 전용).
        /// 기본은 아무 동작 없음.
        /// </summary>
        public virtual void OnHitReceived(HitInfo hitInfo) { }

        /// <summary>즉시 만료 처리 요청 — OnRemove는 StatusController가 다음 처리 시 호출</summary>
        protected void RequestRemoval() => Elapsed = Duration;
    }
}
