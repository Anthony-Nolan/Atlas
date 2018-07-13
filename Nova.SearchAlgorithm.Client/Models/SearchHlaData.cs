using FluentValidation;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class SearchHlaData
    {
        /// <summary>
        /// Search HLA for locus A.
        /// Required.
        /// </summary>
        public LocusSearchHla LocusSearchHlaA { get; set; }
        
        /// <summary>
        /// Search HLA for locus B.
        /// Required.
        /// </summary>
        public LocusSearchHla LocusSearchHlaB { get; set; }
        
        /// <summary>
        /// Search HLA for locus C.
        /// Optional.
        /// </summary>
        public LocusSearchHla LocusSearchHlaC { get; set; }
        
        /// <summary>
        /// Search HLA for locus DQB1.
        /// Optional.
        /// </summary>
        public LocusSearchHla LocusSearchHlaDqb1 { get; set; }
        
        /// <summary>
        /// Search HLA for locus DRB1.
        /// Required.
        /// </summary>
        public LocusSearchHla LocusSearchHlaDrb1 { get; set; }
    }
    
    public class SearchHlaDataValidator : AbstractValidator<SearchHlaData>
    {
        public SearchHlaDataValidator()
        {
            RuleFor(x => x.LocusSearchHlaA).NotNull();
            RuleFor(x => x.LocusSearchHlaB).NotNull();
            RuleFor(x => x.LocusSearchHlaDrb1).NotNull();
        }
    }
}