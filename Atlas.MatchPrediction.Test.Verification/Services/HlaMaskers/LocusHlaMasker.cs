using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface ILocusHlaMasker
    {
        Task<IReadOnlyCollection<SimulantLocusHla>> MaskHla(
            LocusMaskingRequests request, IReadOnlyCollection<SimulantLocusHla> genotypeHlaTypings);
    }

    internal class LocusHlaMasker : ILocusHlaMasker
    {
        private readonly IHlaDeleter hlaDeleter;
        private readonly IHlaConverter hlaConverter;
        private readonly IMacBuilder macBuilder;
        private readonly IXxCodeBuilder xxCodeBuilder;

        public LocusHlaMasker(IHlaDeleter hlaDeleter, IHlaConverter hlaConverter, IMacBuilder macBuilder, IXxCodeBuilder xxCodeBuilder)
        {
            this.hlaDeleter = hlaDeleter;
            this.hlaConverter = hlaConverter;
            this.macBuilder = macBuilder;
            this.xxCodeBuilder = xxCodeBuilder;
        }

        public async Task<IReadOnlyCollection<SimulantLocusHla>> MaskHla(
            LocusMaskingRequests request,
            IReadOnlyCollection<SimulantLocusHla> genotypeHlaTypings)
        {
            if (genotypeHlaTypings.Select(hla => hla.Locus).Distinct().Count() > 1)
            {
                throw new ArgumentException("Multiple loci detected; only one locus can be masked per method call.");
            }

            if (genotypeHlaTypings.Count != request.TotalSimulantCount)
            {
                throw new ArgumentException("Genotype count does not equal expected total simulant count.");
            }

            var proportionSum = request.MaskingRequests?.Sum(r => r.ProportionToMask);
            if (proportionSum == null || proportionSum.Value == 0)
            {
                return genotypeHlaTypings;
            }

            if (proportionSum.Value < 0 || proportionSum.Value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Masking proportions are not in allowable range of between 0 and 100%.");
            }

            return await MaskLocusTypings(request, genotypeHlaTypings);
        }

        private async Task<IReadOnlyCollection<SimulantLocusHla>> MaskLocusTypings(LocusMaskingRequests request, IReadOnlyCollection<SimulantLocusHla> typings)
        {
            var allTransformationResults = new List<TransformationResult>();
            var remainingTypings = typings.ToList();
            var locus = remainingTypings.First().Locus;

            foreach (var maskingRequest in request.MaskingRequests.Where(r => r.ProportionToMask > 0))
            {
                var transformationRequest = new TransformationRequest
                {
                    ProportionToTransform = maskingRequest.ProportionToMask,
                    TotalSimulantCount = request.TotalSimulantCount,
                    Typings = remainingTypings
                };

                var masked = await MaskHla(
                    maskingRequest.MaskingCategory, transformationRequest, request.HlaNomenclatureVersion);

                allTransformationResults.Add(masked);
                remainingTypings = masked.RemainingTypings.ToList();
            }

            var transformedTypings = allTransformationResults.SelectMany(r => r.SelectedTypings);
            return remainingTypings.Concat(transformedTypings).ToList();
        }

        private Task<TransformationResult> MaskHla(
            MaskingCategory category,
            TransformationRequest request,
            string hlaNomenclatureVersion)
        {
            return category switch
            {
                MaskingCategory.Delete => hlaDeleter.DeleteRandomLocusHla(request),
                MaskingCategory.PGroup => hlaConverter.ConvertRandomLocusHla(request, hlaNomenclatureVersion, TargetHlaCategory.PGroup),
                MaskingCategory.Serology => hlaConverter.ConvertRandomLocusHla(request, hlaNomenclatureVersion, TargetHlaCategory.Serology),
                MaskingCategory.MultipleAlleleCode => macBuilder.ConvertRandomLocusHlaToMacs(request),
                MaskingCategory.XxCode => xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(request),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    internal class LocusMaskingRequests
    {
        public IEnumerable<MaskingRequest> MaskingRequests { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public int TotalSimulantCount { get; set; }
    }
}
