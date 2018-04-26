using AutoMapper;
using Nova.SearchAlgorithm.Config;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [SetUpFixture]
    public class StorageSetup
    {
        private StorageEmulator emulator = new StorageEmulator();

        [OneTimeSetUp]
        public void SetUp()
        {
            emulator.Start();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            emulator.Stop();
        }
    }
}
