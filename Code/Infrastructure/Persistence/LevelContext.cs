namespace PickyParking.Infrastructure.Persistence
{
    
    
    
    public sealed class LevelContext
    {
        public byte[] RulesBytes;
        public int RulesByteCount => RulesBytes?.Length ?? 0;

        public void Clear() => RulesBytes = null;
    }
}
