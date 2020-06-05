using System;

namespace Atlas.MultipleAlleleCodeDictionary.Test.DependencyInjection
{
    public static class DependencyInjection
    {
        private static IServiceProvider provider;

        public static IServiceProvider Provider
        {
            get
            {
                if (provider == null)
                {
                    throw new Exception("Provider has not been set up");
                }
                return provider;
            }
            set => provider = value;
        }
    }
}