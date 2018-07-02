using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration
{
    [SetUpFixture]
    public class StorageSetup
    {
        private StorageEmulator tableStorageEmulator = new StorageEmulator();

        [OneTimeSetUp]
        public void SetUp()
        {
            tableStorageEmulator.Start();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            tableStorageEmulator.Stop();
        }
    }
}
