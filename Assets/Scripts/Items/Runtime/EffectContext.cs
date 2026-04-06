namespace _2D_Roguelike
{
    /// <summary>
    /// 효과 실행기(IEffectExecutor)가 Apply/Remove 시 필요한 컨텍스트.
    /// SourceId로 "누가 준 효과인지"를 추적해 정확한 제거가 가능하다.
    /// </summary>
    public class EffectContext
    {
        public string      SourceId    { get; }
        public StatService StatService { get; }

        public EffectContext(string sourceId, StatService statService)
        {
            SourceId    = sourceId;
            StatService = statService;
        }
    }
}
