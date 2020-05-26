using System;
using System.Linq;

namespace Atlas.Common.Utils.Extensions
{
    public static class TypeExtensions
    {
        public static string GetNeatCSharpName<T>()
        {
            return typeof(T).GetNeatCSharpName();
        }

        public static string GetNeatCSharpName(this Type t)
        {
            if (!t.IsGenericType)
            {
                return t.Name;
            }

            var genericArgs = t
                .GetGenericArguments()
                .Select(GetNeatCSharpName)
                .StringJoin(", ");
            string trimmedGenericBaseName = TrimGenericArgMarkerFromTypeName(t.Name);

            return $"{trimmedGenericBaseName}<{genericArgs}>";
        }

        private static string TrimGenericArgMarkerFromTypeName(string typeName)
        {
            var genericMarkerLocation = typeName.IndexOf("`");
            if (genericMarkerLocation > -1)
            {
                return typeName.Substring(0, genericMarkerLocation);
            }

            return typeName;
        }

    }
}
