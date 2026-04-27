namespace _2D_Roguelike
{
    public enum StatType
    {
        MaxHp,
        AttackPower,     // 기본 공격 데미지
        SkillPower,      // 검기 발산 데미지
        RollPower,       // 롤링 슬래시 데미지
        MoveSpeed,
        KnockbackForce,

        // 데미지 타입별 배율 — 기본값은 PlayerStatController에서 설정
        // 아이템·각인으로 Multiply 모디파이어를 추가해 강화
        PhysicalPower,   // 물리 데미지 배율 (기본 1.0)
        MagicPower,      // 마법 데미지 배율 (기본 1.0)
        CriticalPower,   // 치명 데미지 배율 (기본 1.5)
    }
}
