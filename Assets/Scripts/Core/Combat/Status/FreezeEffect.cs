using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 빙결 상태이상.
    /// 적용 시 행동을 일시정지한다 (공격 코루틴은 유지, PauseableWait으로 대기 중).
    /// 피격을 받으면 즉시 해제되고, 해제 후 일시정지했던 행동을 이어서 계속한다.
    /// </summary>
    public class FreezeEffect : StatusEffectBase
    {
        private static readonly Color TintColor = new Color(0.3f, 0.6f, 1f);

        private IStatusLockable _lockable;
        private DamageFlash     _damageFlash;

        public FreezeEffect(StatusController owner, StatusEffectSpec spec)
            : base(owner, StatusEffectType.Freeze, spec.duration) { }

        public override void OnApply()
        {
            _lockable    = Owner.GetComponent<IStatusLockable>();
            _damageFlash = Owner.GetComponent<DamageFlash>();

            _lockable?.ApplyActionLock(cancelOngoing: false);
            _damageFlash?.SetStatusTint(TintColor);
        }

        public override void OnRemove()
        {
            _lockable?.RemoveActionLock(wasCancelled: false);
            _damageFlash?.ClearStatusTint();
        }

        /// <summary>피격 시 즉시 해제 — RequestRemoval() → StatusController가 OnRemove() 호출</summary>
        public override void OnHitReceived(HitInfo hitInfo)
        {
            RequestRemoval();
        }
    }
}
