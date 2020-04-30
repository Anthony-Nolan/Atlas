using System;
using System.Reflection;

namespace Atlas.Utils.Core.Reflection
{
    public static class MemberExtensions
    {
        /// <summary>
        /// Returns a property info for X if given method is a refence to the special get_X,
        /// or the method itself otherwise.
        /// </summary>
        /// <param name="method">Input method</param>
        /// <returns>Property or method info</returns>
        public static MemberInfo GetReflectedInfo(this MethodInfo method)
        {
            if (method.IsSpecialName)
            {
                var name = method.Name;

                // Method getter and setter names always start with get_x and set_x
                // See ECMA 335 II.22.28 points 9 and 10
                // http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
                if (name.StartsWith("get_"))
                {
                    return method.ReflectedType.GetProperty(name.Substring(4));
                }

                // Extend as required for setters, indexers etc
                throw new NotImplementedException();
            }
            return method;
        }
    }
}
