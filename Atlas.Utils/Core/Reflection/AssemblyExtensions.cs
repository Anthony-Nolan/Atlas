using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Atlas.Utils.Core.Reflection
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Assembly> LoadNovaAssemblies(this Assembly assembly, string suffix = null)
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
            var startsWith = suffix != null ? $"Nova.{suffix}" : "Nova";
            return name => name.Name.StartsWith(startsWith);
        }
    }
}
