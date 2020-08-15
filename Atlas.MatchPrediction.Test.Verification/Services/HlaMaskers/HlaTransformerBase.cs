using Atlas.MatchPrediction.Test.Verification.Models;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal abstract class HlaTransformerBase
    {
        protected static async Task<TransformationResult> TransformRandomlySelectedTypings(
            TransformationRequest request,
            Func<string, Task<string>> locusHlaTransformer)
        {
            if (request.NumberToTransform < 0 || request.NumberToTransform > request.Typings.Count)
            {
                throw new ArgumentException($"Supplied {request.Typings.Count} typings, but requested to transform {request.NumberToTransform}.");
            }

            if (request.NumberToTransform == 0)
            {
                return new TransformationResult
                {
                    SelectedTypings = new List<SimulantLocusHla>(),
                    RemainingTypings = request.Typings
                };
            }

            var shuffledTypings = request.Typings.Shuffle().ToList();
            return new TransformationResult
            {
                SelectedTypings = await TransformHla(shuffledTypings.Take(request.NumberToTransform), locusHlaTransformer),
                RemainingTypings = shuffledTypings.Skip(request.NumberToTransform).ToList()
            };
        }

        private static async Task<IReadOnlyCollection<SimulantLocusHla>> TransformHla(
            IEnumerable<SimulantLocusHla> typingsToTransform,
            Func<string, Task<string>> locusHlaTransformer)
        {
            return await Task.WhenAll(typingsToTransform.Select(async typing => new SimulantLocusHla
            {
                Locus = typing.Locus,
                GenotypeSimulantId = typing.GenotypeSimulantId,
                HlaTyping = await typing.HlaTyping.MapAsync(locusHlaTransformer)
            }));
        }
    }

    internal class TransformationRequest
    {
        public int ProportionToTransform { get; set; }
        public int TotalSimulantCount { get; set; }
        public IReadOnlyCollection<SimulantLocusHla> Typings { get; set; }

        public int NumberToTransform => (int)Math.Round(TotalSimulantCount * (decimal)ProportionToTransform / 100);
    }

    internal class TransformationResult
    {
        /// <summary>
        /// Typings that have been selected during the transformation step
        /// (even where no transformation took place, e.g., in the case of "unmodified" typings).
        /// </summary>
        public IReadOnlyCollection<SimulantLocusHla> SelectedTypings { get; set; }

        /// <summary>
        /// Typings that were not selected, and remain available for further transformation.
        /// </summary>
        public IReadOnlyCollection<SimulantLocusHla> RemainingTypings { get; set; }
    }
}
