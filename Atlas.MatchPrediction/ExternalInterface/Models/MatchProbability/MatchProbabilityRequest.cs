using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class SingleDonorMatchProbabilityInput : IdentifiedMatchProbabilityRequest
    {
        public SingleDonorMatchProbabilityInput()
        {
        }

        public SingleDonorMatchProbabilityInput(IdentifiedMatchProbabilityRequest request) : base(request)
        {
        }

        /// <summary>
        /// To be used when running a match prediction request outside of search
        /// </summary>
        public SingleDonorMatchProbabilityInput(MatchProbabilityRequestBase requestWithoutIds) : base(requestWithoutIds)
        {
        }

        /// <summary>
        /// Input for a single, unique donor phenotype (that could map to one or multiple donor Ids)
        /// </summary>
        public DonorInput Donor { get; set; }
    }

    public class MultipleDonorMatchProbabilityInput : IdentifiedMatchProbabilityRequest
    {
        public MultipleDonorMatchProbabilityInput()
        {
        }

        public MultipleDonorMatchProbabilityInput(IdentifiedMatchProbabilityRequest request) : base(request)
        {
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once CollectionNeverUpdated.Global
        /// <summary>
        /// Input for multiple donor phenotypes
        /// </summary>
        public List<DonorInput> Donors { get; set; }

        internal IEnumerable<SingleDonorMatchProbabilityInput> SingleDonorMatchProbabilityInputs =>
            Donors.Select(d => new SingleDonorMatchProbabilityInput(this)
            {
                Donor = d
            });
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
    /// <see cref="MatchProbabilityRequestBase"/> with additional Ids to help track across serialisation boundaries during search.
    /// </summary>
    public class IdentifiedMatchProbabilityRequest : MatchProbabilityRequestBase
    {
        // ReSharper disable once MemberCanBeProtected.Global - Deserialised
        public IdentifiedMatchProbabilityRequest()
        {
        }

        protected IdentifiedMatchProbabilityRequest(IdentifiedMatchProbabilityRequest initial) : base(initial)
        {
            MatchProbabilityRequestId = initial.MatchProbabilityRequestId;
            SearchRequestId = initial.SearchRequestId;
            MatchingAlgorithmDataRefreshRecordId = initial.MatchingAlgorithmDataRefreshRecordId;
        }

        protected IdentifiedMatchProbabilityRequest(MatchProbabilityRequestBase initialWithoutIds) : base(initialWithoutIds)
        {
        }

        /// <summary>
        /// Unique Identifier used for this match probability request, used to identify MPA requests across serialisation boundaries in durable functions
        /// </summary>
        public string MatchProbabilityRequestId { get; set; }

        /// <summary>
        /// Search ID is used to identify uploaded results of the Match Prediction Algorithm
        /// </summary>
        public string SearchRequestId { get; set; }

        /// <summary>
        /// The ID of the matching algorithm data refresh record whose transient donor database the donors were matched against.
        /// Comes from the same record as <see cref="MatchProbabilityRequestBase.MatchingAlgorithmHlaNomenclatureVersion"/>.
        /// Only set for search; `null` when the search was matched before this value was recorded.
        /// </summary>
        public int? MatchingAlgorithmDataRefreshRecordId { get; set; }
    }

    /// <summary>
    /// Contains information needed to run a match probability request, excluding donor data
    /// </summary>
    public abstract class MatchProbabilityRequestBase
    {
        // ReSharper disable once MemberCanBeProtected.Global - Deserialised
        protected MatchProbabilityRequestBase()
        {
        }

        protected MatchProbabilityRequestBase(MatchProbabilityRequestBase initial)
        {
            MatchingAlgorithmHlaNomenclatureVersion = initial.MatchingAlgorithmHlaNomenclatureVersion;
            ExcludedLoci = initial.ExcludedLoci;
            PatientHla = initial.PatientHla;
            PatientFrequencySetMetadata = initial.PatientFrequencySetMetadata;
            UsePrecomputedGenotypeSets = initial.UsePrecomputedGenotypeSets;
        }

        /// <summary>
        /// <inheritdoc cref="MatchPredictionParameters.MatchingAlgorithmHlaNomenclatureVersion"/>
        /// </summary>
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }

        /// <summary>
        /// Match prediction will be run on all loci by default.
        /// Any loci specified here will be ignored at all stages of match probability calculation, and will not have per-locus predictions returned.
        /// </summary>
        public IEnumerable<Locus> ExcludedLoci { get; set; } = new List<Locus>();

        public PhenotypeInfoTransfer<string> PatientHla { get; set; }
        public FrequencySetMetadata PatientFrequencySetMetadata { get; set; }

        /// <summary>
        /// Overrides the precompute kill-switch (<see cref="Settings.PrecomputedGenotypeSetSettings.UsePrecomputedGenotypeSets"/>)
        /// for this request, in both directions. `null` means the kill-switch decides.
        /// Resolved once per donor batch, so turning the kill-switch off also affects a running search that leaves this `null`.
        /// </summary>
        public bool? UsePrecomputedGenotypeSets { get; set; }
    }
}