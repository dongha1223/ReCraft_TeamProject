namespace _2D_Roguelike
{
    /// <summary>
    /// 효과 타입별 전담 실행기 인터페이스.
    /// 새 EffectDefinition 파생 타입이 생기면 이 인터페이스를 구현하는
    /// 전담 Executor를 하나 추가하고 Registry에 등록한다.
    /// </summary>
    public interface IEffectExecutor
    {
        void Apply(EffectContext context, EffectDefinition definition);
        void Remove(EffectContext context, EffectDefinition definition);
    }
}
