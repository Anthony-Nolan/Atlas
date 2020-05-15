using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.NovaHttpClient.Http.Exceptions;
using Atlas.Common.Utils.Http;
using Newtonsoft.Json.Linq;

namespace Atlas.Common.NovaHttpClient.Client
{
    public class HttpErrorParser
    {
        public virtual async Task<bool> ThrowBadRequestException(HttpContent content)
        {
            var caseSensitiveModel = await GetErrorModel<Dictionary<string, object>>(content);
            var model = new Dictionary<string, object>(caseSensitiveModel, StringComparer.InvariantCultureIgnoreCase);
            if (model.ContainsKey("Error"))
            {
                throw new AtlasHttpException(HttpStatusCode.BadRequest, model["Error"] as string);
            }
            if (model.ContainsKey("fieldErrors") || model.ContainsKey("globalErrors"))
            {
                var globalErrors = ReadGlobalErrors(model["globalErrors"] as JArray);
                IList<FieldErrorModel> fieldErrors = ReadFieldErrors(model["fieldErrors"] as JArray);
                throw new AtlasValidationException(globalErrors, fieldErrors);
            }
            throw new AtlasErrorNotRecognisedException();
        }

        public virtual async Task ThrowGenericException(HttpStatusCode statusCode, HttpContent content)
        {
            var errorModel = await GetErrorModel<ErrorsModel>(content);
            if (errorModel.Error != null)
            {
                throw new AtlasHttpException(statusCode, errorModel.Error);
            }
            throw new AtlasErrorNotRecognisedException();
        }

        public virtual async Task ThrowNotFoundException(HttpContent content)
        {
            var contentText = await content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(contentText))
            {
                throw new AtlasNotFoundException("Resource not found");
            }
            var notFoundError = await GetErrorModel<ErrorsModel>(content);
            if (notFoundError.Error != null)
            {
                throw new AtlasNotFoundException(notFoundError.Error);
            }
            throw new AtlasErrorNotRecognisedException();
        }

        private async Task<T> GetErrorModel<T>(HttpContent content)
        {
            try
            {
                return await content.ReadAsAsync<T>();
            }
            catch (Exception e)
            {
                throw new AtlasErrorNotRecognisedException(e);
            }
        }

        private IList<string> ReadGlobalErrors(JArray globalErrors)
        {
            return Enumerable.ToList<string>(globalErrors.Select(o => o.Value<string>()));
        }

        private IList<FieldErrorModel> ReadFieldErrors(JArray fieldErrors)
        {
            return Enumerable.ToList<FieldErrorModel>(fieldErrors.Select(fe => fe.ToObject<FieldErrorModel>()));
        }
    }
}