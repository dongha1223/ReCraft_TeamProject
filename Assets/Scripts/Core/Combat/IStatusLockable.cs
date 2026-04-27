namespace _2D_Roguelike
{
    /// <summary>
    /// 기절·빙결처럼 행동 전체를 잠그는 상태이상을 받을 수 있는 인터페이스.
    ///
    /// cancelOngoing = true  (기절) : 진행 중인 공격 코루틴을 취소하고 잠금
    /// cancelOngoing = false (빙결) : 코루틴은 유지하되 PauseableWait으로 일시정지
    /// </summary>
    public interface IStatusLockable
    {
        void ApplyActionLock(bool cancelOngoing);
        void RemoveActionLock(bool wasCancelled);
    }
}
