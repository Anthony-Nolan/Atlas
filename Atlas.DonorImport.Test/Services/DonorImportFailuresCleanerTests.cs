using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.Services;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorImportFailuresCleanerTests
    {
        private IDonorImportFailureRepository _repository;

        [SetUp]
        public void Setup()
        {
            _repository = Substitute.For<IDonorImportFailureRepository>();
        }

        [Test]
        public async Task DeleteExpiredDonorImportFailures_DeletesFailuresByExpectedCutOffDate()
        {
            const int expiryInDays = 10;
            var cleaner = BuildCleaner(expiryInDays);

            await cleaner.DeleteExpiredDonorImportFailures();

            await _repository.Received()
                .DeleteDonorImportFailuresBefore(Arg.Is<DateTimeOffset>(d => DateTimeOffset.Now.Subtract(d).Days == expiryInDays));
        }

        private IDonorImportFailuresCleaner BuildCleaner(int expiryInDays)
        {
            var failureLogsSettings = new FailureLogsSettings { ExpiryInDays = expiryInDays };
            return new DonorImportFailuresCleaner(_repository, failureLogsSettings);
        }
    }
}
