using System.Reflection;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.Api
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        // Configuration has been set up by the framework via WebHost.CreateDefaultBuilder
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            // TODO: Find a better setup that works for both validation tests and local user-secrets
            if (!env.ContentRootPath.Contains("Test"))
            {
                var builder = new ConfigurationBuilder();
                builder.AddConfiguration(configuration);
                builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                builder.AddUserSecrets(Assembly.GetExecutingAssembly());
                this.configuration = builder.Build();
            }
            else
            {
                this.configuration = configuration;
            }
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterSettings(configuration);
            services.RegisterDataServices();
            services.RegisterDonorClient();
            services.RegisterHlaMetadataDictionary(
                sp => sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString,
                sp => sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.ApiKey,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.BaseUrl,
                sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value.InstrumentationKey
                );
            services.RegisterSearchAlgorithmTypes();

            services.ConfigureSwaggerService();

            services
                .AddMvc(options => { options.EnableEndpointRouting = false; })
                // When using the default System.Text.Json, all properties in `LocusPositionScoreDetails` models were ignored when serialising
                // Until the cause for this has been identified and eliminated, Newtonsoft.Json must be used instead.
                .AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.ConfigureSwagger();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}