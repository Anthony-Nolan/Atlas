using Microsoft.Owin.Testing;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation
{
    public class BaseStepDefinitions
    {
        protected TestServer Server;

        [BeforeScenario]
        public void BeforeScenario()
        {
            Server = TestServer.Create<Startup>();
        }

        [AfterScenario]
        public void AfterScenario()
        {
            Server.Dispose();
        }
    }
}