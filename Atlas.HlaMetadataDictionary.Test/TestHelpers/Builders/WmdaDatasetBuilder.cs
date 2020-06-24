using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using LochNessBuilder;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders
{
    [Builder]
    internal static class WmdaDatasetBuilder
    {
        public static Builder<WmdaDataset> New => Builder<WmdaDataset>.New
            .With(w => w.HlaNomenclatureVersion, "version")
            .With(w => w.Serologies, new List<HlaNom>())
            .With(w => w.Alleles, new List<HlaNom>())
            .With(w => w.PGroups, new List<HlaNomP>())
            .With(w => w.GGroups, new List<HlaNomG>())
            .With(w => w.SerologyToSerologyRelationships, new List<List<RelSerSer>> { new List<RelSerSer>() })
            .With(w => w.AlleleToSerologyRelationships, new List<List<RelDnaSer>> { new List<RelDnaSer>() })
            .With(w => w.ConfidentialAlleles, new List<ConfidentialAllele>())
            .With(w => w.AlleleStatuses, new List<AlleleStatus>())
            .With(w => w.AlleleNameHistories, new List<AlleleNameHistory>())
            .With(w => w.Dpb1TceGroupAssignments, new List<Dpb1TceGroupAssignment>());
    }
}
