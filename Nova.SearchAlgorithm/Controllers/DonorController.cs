using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("donor")]
    public class DonorController : ApiController
    {
        public DonorController()
        {
        }
    }
}
