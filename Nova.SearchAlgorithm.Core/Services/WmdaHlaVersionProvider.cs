namespace Nova.SearchAlgorithm.Services
{
    public interface IWmdaHlaVersionProvider
    {
        string GetHlaDatabaseVersion();
    }

    public class WmdaHlaVersionProvider : IWmdaHlaVersionProvider
    {
        private readonly string hlaDatabaseVersion;

        public WmdaHlaVersionProvider(string hlaDatabaseVersion)
        {
            this.hlaDatabaseVersion = hlaDatabaseVersion;
        }
        
        public string GetHlaDatabaseVersion()
        {
            return hlaDatabaseVersion;
        }
    }
}