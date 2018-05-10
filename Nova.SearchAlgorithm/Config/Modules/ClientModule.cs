using Autofac;
using Nova.HLAService.Client;

namespace Nova.SearchAlgorithm.Config.Modules
{
    public class ClientModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var clientSettingsProvider = new Helpers.ClientSettingsProvider();

            builder.RegisterType<HlaServiceClient>().AsImplementedInterfaces()
                .WithParameter(new NamedParameter("settings", clientSettingsProvider.GetHlaServiceClientSettings())).InstancePerLifetimeScope();
        }
    }
}