using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Validators;
using FluentValidation.Results;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithmValidator
    {
        public ValidationResult ValidateMatchPredictionAlgorithmInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
    }

    internal class MatchPredictionAlgorithmValidator : IMatchPredictionAlgorithmValidator
    {
        public ValidationResult ValidateMatchPredictionAlgorithmInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            return new MatchProbabilityNonDonorValidator().Validate(singleDonorMatchProbabilityInput);
        }
    }
}