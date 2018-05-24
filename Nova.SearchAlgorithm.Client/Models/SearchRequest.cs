using FluentValidation;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models
{

    [FluentValidation.Attributes.Validator(typeof(SearchRequestValidator))]
    public class SearchRequest
    {
        public DonorType SearchType { get; set; }
        public MismatchCriteria MatchCriteria { get; set; }
        public IEnumerable<RegistryCode> RegistriesToSearch { get; set; }
    }

    public class SearchRequestValidator : AbstractValidator<SearchRequest>
    {
        public SearchRequestValidator()
        {
            RuleFor(x => x.SearchType).NotEmpty();
            RuleFor(x => x.MatchCriteria).NotEmpty();
            RuleFor(x => x.RegistriesToSearch).NotEmpty();
        }
    }
}
