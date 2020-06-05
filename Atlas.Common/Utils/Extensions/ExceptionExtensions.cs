using System;

namespace Atlas.Common.Utils.Extensions
{
    public static class ExceptionExtensions
    {
        /// <returns>Inner exception message when available, else the outer exception message.</returns>
        public static string InnermostMessage(this Exception ex)
        {
            return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }
    }
}
