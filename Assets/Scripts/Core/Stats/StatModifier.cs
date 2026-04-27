namespace _2D_Roguelike
{
    public class StatModifier
    {
        public string           SourceId  { get; }
        public ModifierOperation Operation { get; }
        public float            Value     { get; }

        public StatModifier(string sourceId, ModifierOperation operation, float value)
        {
            SourceId  = sourceId;
            Operation = operation;
            Value     = value;
        }
    }
}
