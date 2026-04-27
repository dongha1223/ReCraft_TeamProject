namespace _2D_Roguelike
{
    /// <summary>
    /// 상태이상 on-hit 효과를 적용할 공격 종류.
    /// StatusOnHitEffectDefinition에서 "어떤 공격에 붙이는가"를 지정한다.
    /// </summary>
    public enum OnHitTarget
    {
        /// <summary>기본 공격 (X키)</summary>
        BasicAttack,

        /// <summary>스킬 1 — 검기 발산 (A키)</summary>
        Skill1,

        /// <summary>스킬 2 — 롤링 슬레쉬 (S키)</summary>
        Skill2,

        /// <summary>모든 공격에 적용</summary>
        All
    }
}
