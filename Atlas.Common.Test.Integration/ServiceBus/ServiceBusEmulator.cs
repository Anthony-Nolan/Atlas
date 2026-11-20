using System;
using NUnit.Framework;

namespace Atlas.Common.Test.Integration.ServiceBus;

/// <summary>
/// The Azure Service Bus emulator that the tests in this folder run against. Its own build pipeline job starts it and sets
/// <see cref="EnabledVariable"/>. Without that variable, the tests ignore themselves.
/// </summary>
internal static class ServiceBusEmulator
{
    public const string EnabledVariable = "RUN_SERVICE_BUS_EMULATOR_TESTS";

    public const string ConnectionString =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public const string AdminConnectionString =
        "Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public static void IgnoreUnlessEnabled()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnabledVariable)))
        {
            Assert.Ignore($"Set {EnabledVariable} and start the Service Bus emulator. See README_DevelopmentStartUpGuide.md.");
        }
    }
}
