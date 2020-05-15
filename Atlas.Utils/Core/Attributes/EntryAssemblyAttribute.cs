using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Atlas.Utils.Core.Attributes
{
    /// <summary>
    /// For certain types of apps, such as web apps, <see cref="Assembly.GetEntryAssembly"/>
    /// returns null.  With the <see cref="EntryAssemblyAttribute"/>, we can designate
    /// an assembly as the entry assembly by creating an instance of this attribute,
    /// typically in the AssemblyInfo.cs file.
    /// <example>
    /// [assembly: EntryAssembly]
    /// </example>
    /// </summary>
    /// See: https://stackoverflow.com/a/40269369
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class EntryAssemblyAttribute : Attribute
    {
        /// <summary>
        /// Lazily find the entry assembly.
        /// </summary>
        private static readonly Lazy<Assembly> EntryAssemblyLazy = new Lazy<Assembly>(GetEntryAssemblyLazily);

        /// <summary>
        /// Gets reduced entry assembly.
        /// </summary>
        /// <returns>Just the entry assembly name.</returns>
        public static string GetReducedEntryAssembly()
        {
            var assemblyArray = EntryAssemblyLazy.Value.ToString().Split(',');
            return assemblyArray[0];
        }

        /// <summary>
        /// Invoked lazily to find the entry assembly.  We want to cache this value as it may
        /// be expensive to find.
        /// </summary>
        /// <returns>The entry assembly.</returns>
        private static Assembly GetEntryAssemblyLazily()
        {
            return Assembly.GetEntryAssembly() ?? FindEntryAssemblyInCurrentAppDomain();
        }

        /// <summary>
        /// Finds the entry assembly in the current app domain.
        /// </summary>
        /// <returns>The entry assembly.</returns>
        private static Assembly FindEntryAssemblyInCurrentAppDomain()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var entryAssemblies = new List<Assembly>();
            foreach (var assembly in assemblies)
            {
                // Note the usage of LINQ SingleOrDefault.  The EntryAssemblyAttribute's AttributeUsage
                // only allows it to occur once per assembly; declaring it more than once results in
                // a compiler error.
                var attribute =
                    assembly.GetCustomAttributes().OfType<EntryAssemblyAttribute>().SingleOrDefault();
                if (attribute != null)
                {
                    entryAssemblies.Add(assembly);
                }
            }

            // Note that we use LINQ Single to ensure we found one and only one assembly with the
            // EntryAssemblyAttribute.  The EntryAssemblyAttribute should only be put on one assembly
            // per application.
            return entryAssemblies.Single();
        }
    }
}
