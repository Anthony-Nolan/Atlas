using Microsoft.Owin.Testing;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests
{
    public class ServerManager
    {
        public static TestServer Server { get; set; }

        public static void StartServer()
        {
            Server = TestServer.Create<Startup>();
        }

        public static void StopServer()
        {
            Server.Dispose();
        }
    }
}