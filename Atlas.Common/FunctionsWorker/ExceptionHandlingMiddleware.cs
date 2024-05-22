using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Common.FunctionsWorker
{
    public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch
            {
                var httpContext = context.GetHttpContext();

                if (httpContext is { Response.HasStarted: false })
                {
                    httpContext.Response.StatusCode = 500;
                    await httpContext.Response.CompleteAsync();
                }

                throw;
            }
        }
    }
}
