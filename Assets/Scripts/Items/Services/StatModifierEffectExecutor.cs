namespace _2D_Roguelike
{
    /// <summary>
    /// StatModifierEffectDefinition 전담 실행기.
    /// Apply 시 StatService에 모디파이어를 추가하고,
    /// Remove 시 같은 sourceId로 등록된 해당 스탯 모디파이어를 제거한다.
    /// </summary>
    public class StatModifierEffectExecutor : IEffectExecutor
    {
        public void Apply(EffectContext context, EffectDefinition definition)
        {
            var def = (StatModifierEffectDefinition)definition;
            context.StatService.AddModifier(context.SourceId, def.statType, def.operation, def.value);
        }

        public void Remove(EffectContext context, EffectDefinition definition)
        {
            var def = (StatModifierEffectDefinition)definition;
            context.StatService.RemoveModifiersFromSource(context.SourceId, def.statType);
        }
    }
}
