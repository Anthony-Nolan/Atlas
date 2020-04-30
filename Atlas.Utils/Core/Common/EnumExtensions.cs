using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Atlas.Utils.Core.Common
{
    public static class EnumExtensions
    {
        public static TAttribute GetAttribute<TAttribute>(this IComparable enumValue)
            where TAttribute : Attribute
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<TAttribute>();
        }

        public static string GetDescription(this IComparable enumValue)
        {
            var descriptionAttribute = enumValue.GetAttribute<DescriptionAttribute>();
            return descriptionAttribute != null
                ? descriptionAttribute.Description
                : enumValue.ToString();
        }

        public static string GetEnumMemberValue(this IComparable enumValue)
        {
            var memberAttribute = enumValue.GetAttribute<EnumMemberAttribute>();
            return memberAttribute != null
                ? memberAttribute.Value
                : enumValue.ToString();
        }

        public static List<TEnum> ToExhaustiveList<TEnum>() where TEnum : IComparable
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
        }
    }
}
