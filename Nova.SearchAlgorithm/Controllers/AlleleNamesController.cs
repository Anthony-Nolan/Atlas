using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("allele-names")]
    public class AlleleNamesController : ApiController
    {
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IAlleleNamesLookupService lookupService;

        public AlleleNamesController(IAlleleNamesService alleleNamesService, IAlleleNamesLookupService lookupService)
        {
            this.alleleNamesService = alleleNamesService;
            this.lookupService = lookupService;
        }

        [HttpGet]
        [Route("lookup")]
        public async Task<IEnumerable<string>> GetCurrentAlleleNames(MatchLocus matchLocus, string alleleLookupName)
        {
            return await lookupService.GetCurrentAlleleNames(matchLocus, alleleLookupName);
        }
    }
}
