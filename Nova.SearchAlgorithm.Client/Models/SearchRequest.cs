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

    // TODO:NOVA-756 use the correct and full list of valid registry codes
    public enum RegistryCode
    {
        ANBMT = 1, // Anthony Nolan
        NHSBT = 2, // NHS Blood Transfusion
        WBS = 3, // Welsh Blood Service
        DKMS = 4, // German Marrow Donor Program
    }

    [FluentValidation.Attributes.Validator(typeof(SearchRequestValidator))]
    public class SearchRequest
    {
        public SearchType SearchType { get; set; }
        public MatchCriteria MatchCriteria { get; set; }
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
