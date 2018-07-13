namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups
{
    public interface IPreCalculatedHlaInfo<out T>
    {
        T PreCalculatedHlaInfo { get; }
    }
}
