using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Atlas.Utils.Core.Common;
using Atlas.Utils.Core.Reflection;

namespace Atlas.Utils.Core.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionalAttribute : Attribute
    {
    }

    public class AppSettingsConfigProvider<T> where T : class
    {
        private readonly Lazy<T> instance;
        private char valuesDelimiter = ';';

        public AppSettingsConfigProvider()
        {
            instance = new Lazy<T>(CreateProxy);
        }

        public char ValuesDelimiter
        {
            get { return valuesDelimiter; }
            set
            {
                if (instance.IsValueCreated)
                {
                    throw new InvalidOperationException("The proxy class has already been created.");
                }
                valuesDelimiter = value;
            }
        }

        public T Settings => instance.Value;

        private T CreateProxy()
        {
            return new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(new ConfigInterceptor(valuesDelimiter));
        }

        private class ConfigInterceptor : IInterceptor
        {
            private readonly ConcurrentDictionary<MethodInfo, object> configCache =
                new ConcurrentDictionary<MethodInfo, object>();

            private readonly char valuesDelimiter;

            public ConfigInterceptor(char valuesDelimiter)
            {
                this.valuesDelimiter = valuesDelimiter;
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.ReturnValue = configCache.GetOrAdd(invocation.Method, GetValue);
            }

            private object GetValue(MethodInfo method)
            {
                var reflectedInfo = method.GetReflectedInfo();
                var propertyKey = reflectedInfo.Name;
                var setting = ConfigurationManager.AppSettings[propertyKey];
                var returnType = method.ReturnType;
                if (setting == null)
                {
                    if (returnType.IsPrimitive || reflectedInfo.GetCustomAttribute<OptionalAttribute>() == null)
                    {
                        throw new InvalidOperationException($"App setting '{propertyKey}' not found.");
                    }
                    return null;
                }
                setting = Environment.ExpandEnvironmentVariables(setting);
                return ConvertValue(setting, method.ReturnType);
            }

            private object ConvertValue(string value, Type returnType)
            {
                var type = Nullable.GetUnderlyingType(returnType) ?? returnType;
                if (type == typeof(string))
                {
                    return value;
                }
                if (type == typeof(DateTime))
                {
                    // DateTime objects are expected to be in ISO-8610 format
                    return DateTime.ParseExact(value, "o", CultureInfo.InvariantCulture);
                }
                if (typeof(IEnumerable<>).IsAssignableFromGeneric(returnType))
                {
                    var innerType = returnType
                        .GetInterfaceMatchingGeneric(typeof(IEnumerable<>))
                        .GetGenericArguments()[0];
                    return value.Split(new[] { valuesDelimiter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => ConvertValue(v.Trim(), innerType))
                        .Cast(innerType);
                }
                return type.IsEnum ? Enum.Parse(type, value, true) : Convert.ChangeType(value, type);
            }
        }
    }
}
