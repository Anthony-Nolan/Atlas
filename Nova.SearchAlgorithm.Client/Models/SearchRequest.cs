using FluentValidation;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models
{
    public enum SearchType
    {
        Adult = 1,
        Cord = 2,
        Nima = 3
    }

    public enum RegistryCode
    {
        AN = 1, // Anthony Nolan
        NHSBT = 2, // NHS Blood Transfusion
        WBS = 3, // Welsh Blood Service
        DKMS = 4, // German Marrow Donor Program,
        FRANCE = 5,
        NMDP = 6
    }

    [FluentValidation.Attributes.Validator(typeof(SearchRequestValidator))]
    public class SearchRequest
    {
        public SearchType SearchType { get; set; }
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
