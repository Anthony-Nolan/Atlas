using System;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.SearchAlgorithm.Test.Integration.DependencyInjection
{
    public static class DependencyInjection
    {
        private static ServiceProvider _provider;

        public static ServiceProvider Provider
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