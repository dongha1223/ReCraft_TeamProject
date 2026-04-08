namespace _2D_Roguelike
{
    /// <summary>
    /// StatusOnHitEffectDefinition 전담 실행기.
    /// Apply  시 OnHitStatusRegistry에 등록 → 이후 공격 시 spec이 HitInfo에 포함된다.
    /// Remove 시 sourceId로 등록된 항목을 제거한다.
    /// </summary>
    public class StatusOnHitEffectExecutor : IEffectExecutor
    {
        public void Apply(EffectContext context, EffectDefinition definition)
        {
            var def = (StatusOnHitEffectDefinition)definition;
            context.OnHitRegistry?.Register(def, context.SourceId);
        }

        public void Remove(EffectContext context, EffectDefinition definition)
        {
            context.OnHitRegistry?.Unregister(context.SourceId);
        }
    }
}
