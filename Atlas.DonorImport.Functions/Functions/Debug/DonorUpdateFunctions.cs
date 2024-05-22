using Atlas.Debug.Client.Models.DonorImport;
using Atlas.Common.Utils.Http;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Functions.Models.Debug;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    public class DonorUpdateFunctions
    {
        private readonly IDonorImportFailureRepository donorImportFailureRepository;

        public DonorUpdateFunctions(IDonorImportFailureRepository donorImportFailureRepository)
        {
            this.donorImportFailureRepository = donorImportFailureRepository;
        }

        /// <summary>
        /// Retrieves donor import failures by file name.
        /// File name must include the file extension, e.g., `.json`, but file path is optional.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="fileName">The file name.</param>
        /// <returns>Donor import failures found for the given <paramref name="fileName"/>.</returns>
        [Function(nameof(GetDonorImportFailuresByFileName))]
        [ProducesResponseType(typeof(DonorImportFailureInfo), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonorImportFailuresByFileName(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = $"{RouteConstants.DebugRoutePrefix}/donorUpdates/failures/"+"{fileName}")]
            HttpRequest request,
            string fileName)
        {
            var failures = await donorImportFailureRepository.GetDonorImportFailuresByFileName(fileName);
            var failedUpdates = failures.Select(f => f.ToFailedDonorUpdate()).ToList();

            return new JsonResult(new DonorImportFailureInfo
            {
                FileName = fileName,
                FailedUpdates = failedUpdates,
                FailedUpdateCount = failedUpdates.Count
            });
        }
    }
}