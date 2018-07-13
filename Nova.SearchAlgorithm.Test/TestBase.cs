using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using AutoMapper;
using Nova.SearchAlgorithm.Config;
using Nova.Utils.Auth;
using Nova.Utils.WebApi.Filters;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test
{
    public abstract class TestBase<TClassUnderTest>
    {
        private readonly Dictionary<Type, object> mocks = new Dictionary<Type, object>();
        protected IContainer container;

        [OneTimeSetUp]
        public void SetupContainer()
        {
            container = CreateContainer();
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

        private IContainer CreateContainer()
        {
            var apiKeyProvider = Substitute.For<IApiKeyProvider>();
            var apiKeyAttribute = Substitute.For<ApiKeyRequiredAttribute>(apiKeyProvider);
            var mapper = AutomapperConfig.CreateMapper();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(mapper).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterInstance(apiKeyAttribute).As<ApiKeyRequiredAttribute>().SingleInstance();
            RegisterWithConstructorsAsMocks<TClassUnderTest>(containerBuilder).InstancePerLifetimeScope();
            RegisterDependencies(containerBuilder);
            return containerBuilder.Build();
        }
    }
}
