using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class SingleDonorMatchProbabilityInput : MatchProbabilityRequestInput
    {
        public SingleDonorMatchProbabilityInput()
        {
        }

        public SingleDonorMatchProbabilityInput(MatchProbabilityRequestInput matchProbabilityRequestInput) : base(matchProbabilityRequestInput)
        {
        }

        /// <summary>
        /// Can actually represent multiple donors, provided they all share phenotypes and metadata 
        /// </summary>
        public DonorInput DonorInput { get; set; }
    }

    public class MultipleDonorMatchProbabilityInput : MatchProbabilityRequestInput
    {
        public MultipleDonorMatchProbabilityInput()
        {
            
        }

        public MultipleDonorMatchProbabilityInput(MatchProbabilityRequestInput requestInput) : base(requestInput)
        {
            
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<DonorInput> Donors { get; set; }

        internal IEnumerable<SingleDonorMatchProbabilityInput> SingleDonorMatchProbabilityInputs =>
            Donors.Select(d => new SingleDonorMatchProbabilityInput(this)
            {
                DonorInput = d
            }).ToList();
    }

    public class DonorInput
    {
        /// <summary>
        /// Used to identify results when running the Match Prediction Algorithm in batches.
        /// Also useful for logging purposes.
        ///
        /// Multiple donor ids are possible, as donors with the same phenotype + metadata will give the same MPA results, and should therefore be run together 
        /// </summary>
        public List<int> DonorIds { get; set; }

        public int DonorId
        {
            set => DonorIds = new List<int> {value};
        }
        
        public PhenotypeInfoTransfer<string> DonorHla { get; set; }
        public FrequencySetMetadata DonorFrequencySetMetadata { get; set; }
    }

    /// <summary>
    /// Contains all information to run a match prediction *request* - whether for one donor or multiple
    /// </summary>
    public class MatchProbabilityRequestInput
    {
        // ReSharper disable once MemberCanBeProtected.Global - Deserialised
        public MatchProbabilityRequestInput()
        {
        }

        protected MatchProbabilityRequestInput(MatchProbabilityRequestInput initial)
        {
            SearchRequestId = initial.SearchRequestId;
            ExcludedLoci = initial.ExcludedLoci;
            PatientHla = initial.PatientHla;
            PatientFrequencySetMetadata = initial.PatientFrequencySetMetadata;
            HlaNomenclatureVersion = initial.HlaNomenclatureVersion;
        }

        /// <summary>
        /// Search ID is used to identify uploaded results of the Match Prediction Algorithm
        /// </summary>
        public string SearchRequestId { get; set; }

        /// <summary>
        /// Match prediction will be run on all loci by default.
        /// Any loci specified here will be ignored at all stages of match probability calculation, and will not have per-locus predictions returned.
        /// </summary>
        public IEnumerable<Locus> ExcludedLoci { get; set; } = new List<Locus>();

        public PhenotypeInfoTransfer<string> PatientHla { get; set; }
        public FrequencySetMetadata PatientFrequencySetMetadata { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}