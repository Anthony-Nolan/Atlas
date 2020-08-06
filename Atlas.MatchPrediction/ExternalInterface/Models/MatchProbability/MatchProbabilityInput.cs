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

        public DonorInput Donor { get; set; }
    }

    public class MultipleDonorMatchProbabilityInput : MatchProbabilityRequestInput
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<DonorInput> Donors { get; set; }

        internal IEnumerable<SingleDonorMatchProbabilityInput> SingleDonorMatchProbabilityInputs =>
            Donors.Select(d => new SingleDonorMatchProbabilityInput(this)
            {
                Donor = d
            }).ToList();
    }

    public class DonorInput
    {
        /// <summary>
        /// Donor ID is used to identify results when running the Match Prediction Algorithm in batches
        /// It is also useful for logging purposes.
        /// </summary>
        public int DonorId { get; set; }

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
        /// Search ID is not necessary for running match prediction, but will be useful for logging
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