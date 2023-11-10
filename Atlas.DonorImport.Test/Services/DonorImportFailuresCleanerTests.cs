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
        [Test]
        public async Task DeleteExpiredDonorImportFailures_DeletesFailuresByExpectedCutOffDate()
        {
            const int expiryInDays = 10;
            var failureLogsSettings = new FailureLogsSettings { ExpiryInDays = expiryInDays };
            var repository = Substitute.For<IDonorImportFailureRepository>();
            var cleaner = new DonorImportFailuresCleaner(repository, failureLogsSettings);

            await cleaner.DeleteExpiredDonorImportFailures();

            await repository.Received()
                .DeleteDonorImportFailuresBefore(Arg.Is<DateTimeOffset>(d => DateTimeOffset.Now.Subtract(d).Days == expiryInDays));
        }
    }
}
