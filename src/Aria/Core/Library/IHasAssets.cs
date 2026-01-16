namespace Aria.Core.Library;

public interface IHasAssets
{
    public IReadOnlyCollection<AssetInfo> Assets { get;} 
}