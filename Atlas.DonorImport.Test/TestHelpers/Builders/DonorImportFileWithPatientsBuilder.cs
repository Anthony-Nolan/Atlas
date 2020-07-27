using System.Collections.Generic;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DonorImportFileWithPatientsBuilder
    {
        public static Builder<DonorFileWithPatients> New => Builder<DonorFileWithPatients>.New
            .With(c => c.patients, new List<DonorUpdate>())
            .With(c => c.updateMode, UpdateMode.Differential);
    }
}