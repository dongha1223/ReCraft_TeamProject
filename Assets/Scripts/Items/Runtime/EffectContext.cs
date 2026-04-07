namespace _2D_Roguelike
{
    /// <summary>
    /// 효과 실행기(IEffectExecutor)가 Apply/Remove 시 필요한 컨텍스트.
    /// SourceId로 "누가 준 효과인지"를 추적해 정확한 제거가 가능하다.
    /// </summary>
    public class EffectContext
    {
        public string               SourceId      { get; }
        public StatService          StatService   { get; }

        /// <summary>
        /// 공격 시 상태이상 부여 레지스트리. null이면 on-hit 효과 없음.
        /// StatusOnHitEffectExecutor가 Register/Unregister 시 사용한다.
        /// </summary>
        public OnHitStatusRegistry  OnHitRegistry { get; }

        public EffectContext(string sourceId, StatService statService,
                             OnHitStatusRegistry onHitRegistry = null)
        {
            SourceId      = sourceId;
            StatService   = statService;
            OnHitRegistry = onHitRegistry;
        }
    }
}
