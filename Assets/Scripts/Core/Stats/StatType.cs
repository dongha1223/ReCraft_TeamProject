namespace _2D_Roguelike
{
    public enum StatType
    {
        MaxHp,
        AttackPower,     // 기본 공격 데미지
        SkillPower,      // 검기 발산(SwordEnergy) 데미지
        RollPower,       // 롤링 슬래시 데미지
        MoveSpeed,
        KnockbackForce,

        // 데미지 타입별 배율 — 기본값은 PlayerStatController에서 설정
        // 아이템·각인으로 Multiply 모디파이어를 추가해 강화
        PhysicalPower,   // 물리 데미지 배율 (기본 1.0)
        MagicPower,      // 마법 데미지 배율 (기본 1.0)
        CriticalPower,   // 치명 데미지 배율 (기본 1.5)

        // ── 스킬별 전용 스탯 (기존 값 뒤에 추가해 직렬화 깨짐 방지) ──
        WhirlwindPower,  // 회전베기 데미지
        PrayPower,       // Pray 스킬 전용 (현재 미사용, 예약)

        // ── NPC 선택지 버프 전용 스탯 ──────────────────────────────
        AttackSpeed,     // 공격 쿨타임 배율 (기본 1.0 — 낮을수록 빠름, Multiply로 감소)
        DropRate,        // 아이템 드롭 가중치 배율 (기본 1.0 — Multiply로 증가)
        ExpBonus,        // 경험치(신념) 획득 배율 (기본 1.0 — Multiply로 증가)
    }
}
