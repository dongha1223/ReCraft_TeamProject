namespace _2D_Roguelike
{
    /// <summary>
    /// 피격 가능한 오브젝트 인터페이스.
    /// 공격자는 구체 타입(PlayerStats/EnemyStats)을 알 필요 없이 이 인터페이스만 사용한다.
    /// </summary>
    public interface IDamageable
    {
        bool IsDead       { get; }
        bool IsInvincible { get; }
        void TakeDamage(HitInfo info);
    }
}
