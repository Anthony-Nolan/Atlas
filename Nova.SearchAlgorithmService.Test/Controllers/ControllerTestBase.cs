using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Autofac;
using Autofac.Builder;
using Microsoft.Owin.Testing;
using Newtonsoft.Json.Schema;
using Nova.SearchAlgorithmService.Config;
using Nova.Utils.Auth;
using Nova.Utils.TestUtils.Json;
using Nova.Utils.WebApi.Filters;
using Nova.Utils.WebApi.Owin.Middleware;
using NSubstitute;
using NUnit.Framework;
using Owin;

namespace Nova.SearchAlgorithmService.Test.Controllers
{
    public abstract class ControllerTestBase<TController> where TController : ApiController
    {
        private readonly Dictionary<Type, object> mocks = new Dictionary<Type, object>();
        private IContainer container;

        protected TestServer Server { get; private set; }

        [OneTimeSetUp]
        public void CreateServer()
        {
            container = CreateContainer();
            Server = CreateServerInstance();
        }

        [OneTimeTearDown]
        public void TearDownServer()
        {
            Server?.Dispose();
        }

        [SetUp]
        public void ResetUserAndMocks()
        {
            foreach (var mock in mocks.Values)
            {
                mock.ClearReceivedCalls();
            }
        }

        protected virtual void RegisterDependencies(ContainerBuilder builder)
        {
        }

        protected TMock GetFake<TMock>()
        {
            if (!mocks.ContainsKey(typeof(TMock)))
            {
                throw new InvalidOperationException($"{typeof(TMock).Name} is not a dependency mock");
            }
            return container.Resolve<TMock>();
        }

        protected IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterWithConstructorsAsMocks<T>(ContainerBuilder builder)
        {
            var ctor = typeof(T).GetConstructors().Single();
            foreach (var ctorParam in ctor.GetParameters())
            {
                RegisterMock(builder, ctorParam.ParameterType);
            }
            return builder.RegisterType<T>();
        }

        protected void RegisterMock<T>(ContainerBuilder builder)
        {
            RegisterMock(builder, typeof(T));
        }

        protected void RegisterMock(ContainerBuilder builder, Type mockType)
        {
            if (mocks.ContainsKey(mockType))
            {
                // Already registered this mock
                return;
            }
            var mock = Substitute.For(new[] { mockType }, new object[0]);
            builder.RegisterInstance(mock).AsImplementedInterfaces().SingleInstance();
            mocks[mockType] = mock;
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

        private IContainer CreateContainer()
        {
            var apiKeyProvider = Substitute.For<IApiKeyProvider>();
            var apiKeyAttribute = Substitute.For<ApiKeyRequiredAttribute>(apiKeyProvider);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(TestSetup.Mapper).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterInstance(apiKeyAttribute).As<ApiKeyRequiredAttribute>().SingleInstance();
            RegisterWithConstructorsAsMocks<TController>(containerBuilder).InstancePerLifetimeScope();
            RegisterDependencies(containerBuilder);
            return containerBuilder.Build();
        }

        private TestServer CreateServerInstance()
        {
            Action<IAppBuilder> testConfig = app =>
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
            };
            return TestServer.Create(testConfig);
        }
    }
}
