using System.Collections.Generic;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal class DonorImportFileWithNoUpdateBuilder
    {
        public static Builder<DonorFileWithoutUpdate> New => Builder<DonorFileWithoutUpdate>.New
            .With(c => c.donors, new List<DonorUpdate>());
    }
}