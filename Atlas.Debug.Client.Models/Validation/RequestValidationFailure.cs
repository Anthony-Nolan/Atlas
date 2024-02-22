namespace Atlas.Debug.Client.Models.Validation
{
    /// <summary>
    /// Subset of most useful properties from the `FluentValidation.Results.ValidationFailure` class.
    /// Props must have same name as the original class to be deserialized correctly.
    /// </summary>
    public class RequestValidationFailure
    {
        /// <summary>Property that failed validation.</summary>
        public string PropertyName { get; set; }

        /// <summary>The error message</summary>
        public string ErrorMessage { get; set; }

        /// <summary>The property value that caused the failure.</summary>
        public object AttemptedValue { get; set; }
    }
}
