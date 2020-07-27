using System.Collections.Generic;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    internal class DonorImportFileWithNoDonorsBuilder
    {
        public static Builder<DonorFileWithoutDonor> New => Builder<DonorFileWithoutDonor>.New
            .With(c => c.updateMode, UpdateMode.Differential);
    }
}