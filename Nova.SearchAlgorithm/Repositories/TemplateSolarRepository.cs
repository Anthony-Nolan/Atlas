using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Client.Filters;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Solar.Connection;
using Nova.Utils.Pagination;

namespace Nova.SearchAlgorithm.Repositories
{
    public interface ITemplateSolarRepository
    {
        Task<TemplateModel> Get(string id);
        Task<PaginatedModel<TemplateModel>> Search(TemplateFilterModel filter, PaginationData pagination);
    }

    public class TemplateSolarRepository : ITemplateSolarRepository
    {
        private readonly ISolarConnectionFactory factory;

        public TemplateSolarRepository(ISolarConnectionFactory factory)
        {
            this.factory = factory;
        }

        public Task<TemplateModel> Get(string id)
        {
            const string sql = "SELECT 1 FROM dual";
            using (var connection = factory.GetConnection())
            {
                return connection.QueryFirstOrDefaultAsync<TemplateModel>(sql);
            }
        }

        public Task<PaginatedModel<TemplateModel>> Search(TemplateFilterModel filter, PaginationData pagination)
        {
            throw new System.NotImplementedException();
        }
    }
}