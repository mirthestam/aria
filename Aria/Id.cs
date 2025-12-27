namespace Aria;

public abstract class Id(string key = "UNKNOWN")
{
    public abstract class TypedId<TId>(TId id, string key) : Id(key)
    {
        public TId Value => id;
        
        protected override string GetId()
        {
            return id?.ToString() ?? string.Empty;
        }
    }
    
    // TODO: for comparison, we need to compare the ToString
    // as  this is supposed to behave like a value object
    
    protected abstract string GetId();
    
    public sealed override string ToString()
    {
        return $"{key}::{GetId()}";
    }
}