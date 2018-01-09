using System;
using System.Linq;
using System.Web.Http;
using Autofac;
using FluentValidation;
using FluentValidation.WebApi;
using Nova.Utils.Common;

namespace Nova.SearchAlgorithm.Config
{
    public static class ValidatorConfig
    {
        public static void ConfigureValidation(this HttpConfiguration config, IContainer container)
        {
            FluentValidationModelValidatorProvider.Configure(config);

            var baseResolver = ValidatorOptions.PropertyNameResolver;
            ValidatorOptions.PropertyNameResolver = (type, memberInfo, expression) =>
            {
                var sep = ValidatorOptions.PropertyChainSeparator;
                var nameParts = baseResolver(type, memberInfo, expression)
                    .Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                // Convert the default name (which uses TitleCase property names) to camelCase.
                // If the name has multiple parts, each one needs to be camelCased.
                return string.Join(sep, nameParts.Select(n => n.ToCamelCase()));
            };
        }
    }
}