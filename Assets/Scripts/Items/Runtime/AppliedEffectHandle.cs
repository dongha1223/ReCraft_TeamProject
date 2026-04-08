namespace _2D_Roguelike
{
    /// <summary>
    /// 현재 적용 중인 효과를 추적하는 핸들.
    /// 제거 시 어떤 EffectDefinition을 어떤 소스로 적용했는지 알 수 있다.
    /// </summary>
    public class AppliedEffectHandle
    {
        public string           SourceId   { get; }
        public EffectDefinition Definition { get; }

        public AppliedEffectHandle(string sourceId, EffectDefinition definition)
        {
            SourceId   = sourceId;
            Definition = definition;
        }
    }
}
