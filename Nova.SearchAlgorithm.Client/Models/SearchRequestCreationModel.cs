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

    [FluentValidation.Attributes.Validator(typeof(SearchRequestCreationModelValidator))]
    public class SearchRequestCreationModel
    {
        public SearchType SearchType { get; set; }
        public MismatchCriteria MismatchCriteria { get; set; }
        public IEnumerable<string> RegistriesToSearch { get; set; }
    }

    public class SearchRequestCreationModelValidator : AbstractValidator<SearchRequestCreationModel>
    {
        public SearchRequestCreationModelValidator()
        {
            RuleFor(x => x.SearchType).NotEmpty();
            RuleFor(x => x.MismatchCriteria).NotEmpty();
            RuleFor(x => x.RegistriesToSearch).NotEmpty();
        }
    }
}
