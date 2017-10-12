using System.Threading.Tasks;
using AutoMapper;
using Nova.TemplateService.Client.Filters;
using Nova.TemplateService.Client.Models;
using Nova.TemplateService.Repositories;
using Nova.Utils.Pagination;

namespace Nova.TemplateService.Services
{
    public interface ITemplateService
    {
        Task<TemplateResponseModel> Get(string id);
    }

    public class TemplateService : ITemplateService
    {
        private readonly IMapper mapper;
        private readonly ITemplateSolarRepository repository;

        public TemplateService(IMapper mapper, ITemplateSolarRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<TemplateResponseModel> Get(string id)
        {
            var entity = await repository.Get(id);
            return mapper.Map<TemplateResponseModel>(entity);
        }

        public async Task<PaginatedModel<TemplateResponseModel>> Search(TemplateFilterModel filter, PaginationData pagination)
        {
            var entities = await repository.Search(filter, pagination);
            return mapper.Map<PaginatedModel<TemplateResponseModel>>(entities);
        }
    }
}