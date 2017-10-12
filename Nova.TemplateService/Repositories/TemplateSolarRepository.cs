using System.Threading.Tasks;
using Dapper;
using Nova.TemplateService.Client.Filters;
using Nova.TemplateService.Client.Models;
using Nova.TemplateService.Solar.Connection;
using Nova.Utils.Pagination;

namespace Nova.TemplateService.Repositories
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