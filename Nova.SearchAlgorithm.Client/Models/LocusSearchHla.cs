using FluentValidation;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class LocusSearchHla
    {
        /// <summary>
        /// String representation of the 1st search HLA type position at this locus.
        /// </summary>
        public string SearchHla1 { get; set; }

        /// <summary>
        /// String representation of the 2nd search HLA type position at this locus.
        /// </summary>
        public string SearchHla2 { get; set; }
    }
    
    public class LocusSearchHlaValidator : AbstractValidator<LocusSearchHla>
    {
        public LocusSearchHlaValidator()
        {
            RuleFor(x => x.SearchHla1).NotEmpty();
            RuleFor(x => x.SearchHla1).NotEmpty();
        }
    }
}