using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Utils.Core.Common;

namespace Atlas.Utils.Core.Reflection
{
    public static class TypeExtensions
    {
        public static bool IsAssignableFromGeneric(this Type type, Type fromType)
        {
            if (type.IsGenericType && !type.IsConstructedGenericType)
            {
                if (type.IsInterface)
                {
                    return fromType.GetInterfaceMatchingGeneric(type) != null;
                }
                var typeToCheck = fromType;
                while (typeToCheck != null)
                {
                    if (typeToCheck.GenericTypeMatches(type))
                    {
                        return true;
                    }
                    typeToCheck = typeToCheck.BaseType;
                }
                return false;
            }
            return type.IsAssignableFrom(fromType);
        }

        public static Type GetInterfaceMatchingGeneric(this Type fromType, Type interfaceType)
        {
            fromType.AssertArgumentNotNull(nameof(fromType));
            interfaceType
                .AssertArgumentNotNull(nameof(interfaceType))
                .AssertArgument(t => t.IsInterface, $"{interfaceType} is not an interface", nameof(interfaceType));

            return fromType
                .GetUniqueInterfaces()
                .FirstOrDefault(it => it.GenericTypeMatches(interfaceType));
        }

        /// <summary>
        ///     Get all interfaces uniquely implemented by type.
        ///     Includes the type itself if the type is an interface
        /// </summary>
        /// <param name="type">Given type</param>
        /// <returns>list of all implemented interfaces including itself if necessary</returns>
        public static IEnumerable<Type> GetUniqueInterfaces(this Type type)
        {
            var types = new HashSet<Type>();

            if (type.IsInterface)
            {
                types.Add(type);
                yield return type;
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (types.Contains(interfaceType))
                {
                    continue;
                }
                types.Add(interfaceType);
                yield return interfaceType;
            }
        }

        private static bool GenericTypeMatches(this Type concreteType, Type genericType)
        {
            return concreteType.IsGenericType && concreteType.GetGenericTypeDefinition() == genericType;
        }
    }
}
