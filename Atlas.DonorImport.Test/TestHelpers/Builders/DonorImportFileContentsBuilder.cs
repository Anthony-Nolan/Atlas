using System.Collections.Generic;
using System.Linq;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DonorImportFileContentsBuilder
    {
        public static Builder<SerialisableDonorImportFileContents> New => Builder<SerialisableDonorImportFileContents>.New
            .With(c => c.donors, new List<DonorUpdate>())
            .With(c => c.updateMode, UpdateMode.Differential);

        public static Builder<SerialisableDonorImportFileContents> WithDonors(
            this Builder<SerialisableDonorImportFileContents> builder,
            int numberOfDonors)
        {
            // Note we cannot use the LochNessBuilder "build x" functionality, as all donors must have unique ids.
            var donors = Enumerable.Range(0, numberOfDonors).Select(i => DonorUpdateBuilder.New.Build());
            return builder.With(c => c.donors, donors);
        }
        
        public static Builder<SerialisableDonorImportFileContents> WithDonors(
            this Builder<SerialisableDonorImportFileContents> builder,
            params DonorUpdate[] donors)
        {
            return builder.With(c => c.donors, donors);
        }
    }
}