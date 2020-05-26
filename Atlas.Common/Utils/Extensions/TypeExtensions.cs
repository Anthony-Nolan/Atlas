using System;
using System.Linq;

namespace Atlas.Common.Utils.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// A standard list of strings should return "List&lt;String&gt;".
        /// <br/>
        /// As opposed to, for example, "List`1", or "System.Collections.Generic.List`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
        /// </summary>
        public static string GetNeatCSharpName<T>()
        {
            return typeof(T).GetNeatCSharpName();
        }

        /// <inheritdoc cref="GetNeatCSharpName{T}"/>
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

        /// <summary>
        /// Converts "List`1" to simply "List"
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
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
