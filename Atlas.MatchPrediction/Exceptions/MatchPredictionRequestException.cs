using System;

namespace Atlas.MatchPrediction.Exceptions
{
    public class MatchPredictionRequestException : Exception
    {
        private const string ErrorMessage = "Error when processing match prediction request.";

        public MatchPredictionRequestException(Exception inner) : base(ErrorMessage, inner)
        {
        }
    }
}
