using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Validators;
using FluentValidation.Results;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionValidator
    {
        /// <summary>
        /// Only validates match probability input properties that are not related to donor data.
        /// </summary>
        public ValidationResult ValidateMatchProbabilityNonDonorInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
    }

    internal class MatchPredictionValidator : IMatchPredictionValidator
    {
        public ValidationResult ValidateMatchProbabilityNonDonorInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            return new MatchProbabilityNonDonorValidator().Validate(singleDonorMatchProbabilityInput);
        }
    }
}