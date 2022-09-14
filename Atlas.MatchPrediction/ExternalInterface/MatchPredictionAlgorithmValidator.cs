using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Validators;
using FluentValidation.Results;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionValidator
    {
        public ValidationResult ValidateMatchProbabilityInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
        public ValidationResult ValidateMatchProbabilityNonDonorInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
    }

    internal class MatchPredictionValidator : IMatchPredictionValidator
    {
        public ValidationResult ValidateMatchProbabilityInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            return new MatchProbabilityInputValidator().Validate(singleDonorMatchProbabilityInput);
        }

        public ValidationResult ValidateMatchProbabilityNonDonorInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            return new MatchProbabilityNonDonorValidator().Validate(singleDonorMatchProbabilityInput);
        }
    }
}