namespace _2D_Roguelike
{
    /// <summary>
    /// 공격 1회를 식별하는 단조 증가 ID 생성기.
    /// 0은 "미설정"을 의미하므로 Next()는 항상 1 이상을 반환한다.
    /// </summary>
    public static class AttackIdGenerator
    {
        private static int _current;
        public static int Next() => ++_current;
    }
}
