namespace _2D_Roguelike
{
    /// <summary>
    /// 범위형 스킬의 탐색 형태.
    /// AreaSkillSpec.ShapeType에서 사용한다.
    /// </summary>
    public enum AreaShapeType
    {
        Circle,  // 원형  — Physics2D.OverlapCircleAll
        Box,     // 직사각형 — Physics2D.OverlapBoxAll
        Cone,    // 부채꼴 — OverlapCircleAll + 각도 필터
    }
}
