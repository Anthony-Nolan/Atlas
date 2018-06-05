using FluentValidation;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Client.Models
{
    [FluentValidation.Attributes.Validator(typeof(SearchRequestValidator))]
    public class SearchRequest
    {
        /// <summary>
        /// The type of donors to search, e.g. Adult or Cord.
        /// </summary>
        public DonorType SearchType { get; set; }

        /// <summary>
        /// Search and mismatch information including search HLA to match on at various loci,
        /// and number of mismatches permitted per donor and per loci.
        /// </summary>
        public MismatchCriteria MatchCriteria { get; set; }

        /// <summary>
        /// List of donor registries to search.
        /// </summary>
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
