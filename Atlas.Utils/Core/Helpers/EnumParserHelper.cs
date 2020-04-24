using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Atlas.Utils.Core.Common;
using Atlas.Utils.Core.Http.Exceptions;

namespace Atlas.Utils.Core.Helpers
{
    public class EnumParserHelper<TEnum> where TEnum : struct, IComparable
    {
        public static TEnum? Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            try
            {
                return EnumExtensions.ToExhaustiveList<TEnum>().Single(
                    member => member.GetEnumMemberValue() == value);
            }
            catch (InvalidOperationException)
            {
                throw new NovaHttpException(HttpStatusCode.BadRequest, $"Unrecognised {typeof(TEnum)}: {value}");
            }
        }

        public static bool TryParse(string value, out TEnum? result)
        {
            List<TEnum> matches;
            try
            {
                matches = EnumExtensions.ToExhaustiveList<TEnum>()
                    .Where(member => member.GetEnumMemberValue() == value)
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                throw new NotImplementedException(
                    $"Enum {typeof(TEnum).Name} does not have EnumMemberAttributes set up correctly");
            }

            if (matches.Any())
            {
                result = matches.First();
                return true;
            }

            result = null;
            return false;
        }
    }
}
