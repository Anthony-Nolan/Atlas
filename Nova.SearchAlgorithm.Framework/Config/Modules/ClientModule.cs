using Autofac;
using Nova.DonorService.Client;
using Nova.HLAService.Client;

namespace Nova.SearchAlgorithm.Config.Modules
{
    /// <summary>
    /// Registers service clients for other Nova microservices.
    /// </summary>
    public class ClientModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HlaServiceClient>().AsImplementedInterfaces()
                .WithParameter(new NamedParameter("settings", ClientConfig.GetHlaServiceClientSettings())).InstancePerLifetimeScope();
            builder.RegisterType<DonorServiceClient>().AsImplementedInterfaces()
                .WithParameter(new NamedParameter("settings", ClientConfig.GetDonorServiceClientSettings())).InstancePerLifetimeScope();
        }
    }
}