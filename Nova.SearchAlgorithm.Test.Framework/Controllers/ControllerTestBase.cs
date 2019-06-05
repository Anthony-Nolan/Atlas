using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Owin.Testing;
using Newtonsoft.Json.Schema;
using Nova.SearchAlgorithm.Config;
using Nova.Utils.TestUtils.Json;
using Nova.Utils.WebApi.Owin.Middleware;
using NUnit.Framework;
using Owin;

namespace Nova.SearchAlgorithm.Test.Controllers
{
    public abstract class ControllerTestBase<TController> : TestBase<TController> where TController : ApiController
    {
        protected TestServer Server { get; private set; }

        [OneTimeSetUp]
        public void CreateServer()
        {
            Server = CreateServerInstance();
        }

        [OneTimeTearDown]
        public void TearDownServer()
        {
            Server?.Dispose();
        }

        protected JSchema LoadSchema(string path)
        {
            return JsonUtils.LoadSchemaFromResource(typeof(ControllerTestBase<>).Assembly,
                $"Resources/Controllers/{path}");
        }

        protected StringContent LoadContent(string path)
        {
            return JsonUtils.LoadJsonContent(typeof(ControllerTestBase<>).Assembly, $"Resources/Controllers/{path}");
        }

        private TestServer CreateServerInstance()
        {
            void TestConfig(IAppBuilder app)
            {
                app.ConfigureAutofac(container);
                app.HandleAllExceptions(JsonConfig.GlobalSettings);
                app.Use((context, next) =>
                {
                    context.Set("server.IsLocal", true);
                    return next();
                });

                var apiConfig = WebApiConfig.CreateConfig(container);
                app.UseWebApi(apiConfig);
                app.UseAutofacWebApi(apiConfig);
            }
            return TestServer.Create(TestConfig);
        }
    }
}
