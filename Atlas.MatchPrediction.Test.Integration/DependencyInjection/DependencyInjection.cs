using System;

namespace Atlas.MatchPrediction.Test.Integration.DependencyInjection
{
    internal static class DependencyInjection
    {
        private static IServiceProvider _provider;

        public static IServiceProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    throw new Exception("Provider has not been set up");
                }
                return _provider;
            }
            set => _provider = value;
        }
    }
}