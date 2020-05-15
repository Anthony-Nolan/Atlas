using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Atlas.Utils.Core.Reflection
{
    public static class AssemblyExtensions
    {
        private const string AtlasAssemblyName = "Atlas";

        public static IEnumerable<Assembly> LoadAtlasAssemblies(this Assembly assembly, string suffix = null)
        {
            if (AssemblyNameMatches(suffix)(assembly.GetName()))
            {
                yield return assembly;
            }
            foreach (var name in assembly.GetReferencedAssemblies().Where(AssemblyNameMatches(suffix)))
            {
                yield return Assembly.Load(name);
            }
        }

        private static Func<AssemblyName, bool> AssemblyNameMatches(string suffix)
        {
            var startsWith = suffix != null ? $"{AtlasAssemblyName}.{suffix}" : AtlasAssemblyName;
            return name => name.Name.StartsWith(startsWith);
        }
    }
}
