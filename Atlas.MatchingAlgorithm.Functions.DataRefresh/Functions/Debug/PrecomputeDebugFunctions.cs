using System.IO;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.Utils.Http;
using Atlas.Common.Validation;
using Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Functions.Debug;

/// <summary>
/// Debug endpoints for the precomputed genotype sets. They do only what SQL cannot: imputation, the typing key and the
/// encoding of the stored data.
/// </summary>
/// <remarks>
/// In this app, not in the matching algorithm app that runs searches. One call can impute up to 80 sets, and each frequency
/// set that it loads stays in the app's cache, so in the matching app it would compete with searches for CPU and memory.
/// This app already writes the stored sets (the precomputation stage and the queued donor updates), and it runs no
/// searches.
/// </remarks>
public class PrecomputeDebugFunctions
{
    private readonly IGenotypeSetDebugService genotypeSetDebugService;

    public PrecomputeDebugFunctions(IGenotypeSetDebugService genotypeSetDebugService)
    {
        this.genotypeSetDebugService = genotypeSetDebugService;
    }

    /// <summary>
    /// Computes the genotype set of a typing, or of up to 20 donors, live, with the engine of the pre-computation and of
    /// search. Returns the typing key, the frequency set, the genotype count, the sum of likelihoods and the size of the
    /// stored data, and the genotypes on request. With <c>Save</c>, it also stores the sets in the active matching
    /// database, where the app setting <c>Debug:AllowPrecomputeWrites</c> allows it. With <c>CompareWithStored</c>, it
    /// compares each set with its stored row, in the active or the dormant database, and gives a parity result.
    /// </summary>
    /// <remarks>
    /// Each donor is imputed once for each locus combination, which can be slow. The request must finish within the HTTP
    /// time limit of 230 seconds.
    /// </remarks>
    [Function(nameof(ComputeGenotypeSets))]
    public async Task<IActionResult> ComputeGenotypeSets(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/precompute/genotypeSets/compute")]
        HttpRequest httpRequest)
    {
        ComputeGenotypeSetsRequest request;
        try
        {
            request = JsonConvert.DeserializeObject<ComputeGenotypeSetsRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
        }
        catch (JsonException exception)
        {
            return new BadRequestObjectResult($"The request body is not valid JSON: {exception.Message}");
        }

        if (request == null)
        {
            return new BadRequestObjectResult("The request has no body.");
        }

        try
        {
            var result = await genotypeSetDebugService.Compute(request.ToGenotypeSetDebugRequest());
            return new JsonResult(ComputeGenotypeSetsResponse.From(result));
        }
        catch (ValidationException exception)
        {
            return new BadRequestObjectResult(exception.ToValidationErrorsModel());
        }
        catch (PrecomputeWritesNotAllowedException exception)
        {
            return new ObjectResult(exception.Message) { StatusCode = (int)HttpStatusCode.Forbidden };
        }
        catch (GenotypeSetDebugUnavailableException exception)
        {
            return new ConflictObjectResult(exception.Message);
        }
    }
}
