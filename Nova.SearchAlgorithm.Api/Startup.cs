using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.DependencyInjection;

namespace Nova.SearchAlgorithm.Api
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        // Configuration has been set up by the framework via WebHost.CreateDefaultBuilder
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterSettings(configuration);
            services.RegisterSearchAlgorithmTypes();
            services.RegisterAllMatchingDictionaryTypes();
            services.RegisterDataServices();
            services.RegisterClients();

            services.AddMvc(options => { options.EnableEndpointRouting = false; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}