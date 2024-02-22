using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.Validation
{
    /// <summary>
    /// Represents a response from a function that has undergone validation.
    /// </summary>
    public class ResponseFromValidatedRequest<TResponse>
    {
        /// <summary>
        /// Was the request successful.
        /// </summary>
        public bool WasSuccess { get; set; }

        /// <summary>
        /// The response if <see cref="WasSuccess"/> is true.
        /// </summary>
        public TResponse ResponseOnSuccess { get; }

        /// <summary>
        /// Validation failures if <see cref="WasSuccess"/> is false due to invalid request.
        /// </summary>
        public IEnumerable<RequestValidationFailure> ValidationFailures { get; }

        /// <summary>
        /// Use for successful requests.
        /// </summary>
        public ResponseFromValidatedRequest(TResponse responseOnSuccess)
        {
            WasSuccess = true;
            ResponseOnSuccess = responseOnSuccess;
        }

        /// <summary>
        /// Use for invalid requests.
        /// </summary>
        /// <param name="validationFailures"></param>
        public ResponseFromValidatedRequest(IEnumerable<RequestValidationFailure> validationFailures)
        {
            WasSuccess = false;
            ValidationFailures = validationFailures;
        }
    }
}
