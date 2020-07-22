using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Atlas.MatchPrediction.Test.Verification
{
    public static class SwaggerStartup
    {
        public static IServiceCollection ConfigureSwaggerService(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo {Title = "My API", Version = "v1"});
            });
            return services;
        }

        public static IApplicationBuilder ConfigureSwagger(this IApplicationBuilder app, IWebHostEnvironment env = null)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty;
                c.ConfigObject.DisplayRequestDuration = true;
            });
            return app;
        }
    }
}