namespace Nova.SearchAlgorithm.Test.Integration.Integration
{
    public interface IDonorIdGenerator
    {
        int NextId();
    }

    /// <summary>
    /// Shared auto-incrementer to generate unique donor ids across all integration test fixtures
    /// </summary>
    public class DonorIdGenerator: IDonorIdGenerator
    {
        private int nextId;
        
        public int NextId()
        {
            return ++nextId;
        }
    }
}