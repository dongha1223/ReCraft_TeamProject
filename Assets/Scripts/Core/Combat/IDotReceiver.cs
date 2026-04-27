namespace _2D_Roguelike
{
    /// <summary>
    /// 상태이상 틱 데미지(DoT)를 받을 수 있는 오브젝트 인터페이스.
    /// TakeDotDamage는 넉백·무적·피격 애니메이션 없이 체력만 깎는다.
    /// </summary>
    public interface IDotReceiver
    {
        bool IsDead { get; }
        void TakeDotDamage(float amount);
    }
}
