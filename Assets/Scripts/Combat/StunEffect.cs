namespace _2D_Roguelike
{
    /// <summary>
    /// 기절 상태이상.
    /// 적용 시 진행 중인 공격 코루틴을 취소하고 모든 행동을 잠근다.
    /// 해제 후 AI가 상황을 재판단해 새 행동을 시작한다.
    /// </summary>
    public class StunEffect : StatusEffectBase
    {
        private IStatusLockable _lockable;

        public StunEffect(StatusController owner, StatusEffectSpec spec)
            : base(owner, StatusEffectType.Stun, spec.duration) { }

        public override void OnApply()
        {
            _lockable = Owner.GetComponent<IStatusLockable>();
            _lockable?.ApplyActionLock(cancelOngoing: true);
        }

        public override void OnRemove()
        {
            _lockable?.RemoveActionLock(wasCancelled: true);
        }
    }
}
