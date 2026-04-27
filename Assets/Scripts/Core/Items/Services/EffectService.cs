namespace _2D_Roguelike
{
    /// <summary>
    /// 효과 적용/제거의 유일한 진입점.
    /// Registry를 통해 적절한 Executor를 찾아 위임하고,
    /// 나중에 Remove할 수 있도록 AppliedEffectHandle을 반환한다.
    /// </summary>
    public class EffectService
    {
        private readonly EffectExecutorRegistry _registry;

        public EffectService(EffectExecutorRegistry registry)
        {
            _registry = registry;
        }

        public AppliedEffectHandle Apply(EffectContext context, EffectDefinition definition)
        {
            var executor = _registry.GetExecutor(definition);
            executor.Apply(context, definition);
            return new AppliedEffectHandle(context.SourceId, definition);
        }

        public void Remove(EffectContext context, AppliedEffectHandle handle)
        {
            var executor = _registry.GetExecutor(handle.Definition);
            executor.Remove(context, handle.Definition);
        }
    }
}
