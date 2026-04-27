namespace _2D_Roguelike
{
    /// <summary>
    /// 데미지 유형.
    /// StatService의 PhysicalPower / MagicPower / CriticalPower 배율에 연결된다.
    /// 적이 플레이어에게 주는 피해는 Physical로 고정하는 것이 기본 정책.
    /// </summary>
    public enum DamageType
    {
        Physical,   // 물리 — StatType.PhysicalPower 배율 적용
        Magic,      // 마법 — StatType.MagicPower 배율 적용
        Critical,   // 치명 — StatType.CriticalPower 배율 적용 (항상 치명타인 스킬에 사용)
    }
}
