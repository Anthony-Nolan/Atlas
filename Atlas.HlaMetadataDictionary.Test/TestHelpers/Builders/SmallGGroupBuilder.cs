using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using LochNessBuilder;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders
{
    [Builder]
    internal static class SmallGGroupBuilder
    {
        private const string DefaultName = "small-g-group";

        public static Builder<SmallGGroup> Default => Builder<SmallGGroup>.New
            .With(x => x.Locus, Locus.A)
            .With(x => x.Name, DefaultName)
            .With(x => x.Alleles, new List<string>());

        public static Builder<SmallGGroup> WithAllele(this Builder<SmallGGroup> builder, string allele)
        {
            return builder.With(x => x.Alleles, new[] { allele });
        }
    }
}
