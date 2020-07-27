using System.Collections.Generic;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models;
using Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DonorImportFileContentsBuilder
    {
        public static Builder<SerialisableDonorImportFileContents> New => Builder<SerialisableDonorImportFileContents>.New
            .With(c => c.donors, new List<DonorUpdate>())
            .With(c => c.updateMode, UpdateMode.Differential);

        public static Builder<SerialisableDonorImportFileContents> WithDonorCount(
            this Builder<SerialisableDonorImportFileContents> builder,
            int numberOfDonors)
        {
            var donors = DonorUpdateBuilder.New.Build(numberOfDonors);
            return builder.With(c => c.donors, donors);
        }
        
        public static Builder<SerialisableDonorImportFileContents> WithDonors(
            this Builder<SerialisableDonorImportFileContents> builder,
            params DonorUpdate[] donors)
        {
            return builder.With(c => c.donors, donors);
        }
    }
    
    [Builder]
    internal static class DonorImportFileWithNoDonorsBuilder
    {
        public static Builder<DonorFileWithoutDonor> New => Builder<DonorFileWithoutDonor>.New
            .With(c => c.updateMode, UpdateMode.Differential);
    }
    
    [Builder]
    internal static class DonorImportFileWithNoUpdateBuilder
    {
        public static Builder<DonorFileWithoutUpdate> New => Builder<DonorFileWithoutUpdate>.New
            .With(c => c.donors, new List<DonorUpdate>());
    }
    
    [Builder]
    internal static class DonorImportFileWithUnexpectedFieldBuilder
    {
        public static Builder<DonorFileWithUnexpectedField> New => Builder<DonorFileWithUnexpectedField>.New
            .With(c => c.unexpectedField, new List<DonorUpdate>())
            .With(c => c.updateMode, UpdateMode.Differential);
    }
    
}