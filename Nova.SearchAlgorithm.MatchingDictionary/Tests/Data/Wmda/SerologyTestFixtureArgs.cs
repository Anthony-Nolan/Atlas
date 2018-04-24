using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Data.Wmda
{
    public class SerologyTestFixtureArgs
    {
        public static object[] Args = {
            new object[] { SerologyFilter.Instance.Filter, new[] { "A", "B", "Cw", "DQ", "DR" } }
        };
    }
}
