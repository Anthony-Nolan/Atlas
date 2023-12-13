using Atlas.DonorImport.Data.Models;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    public static class DatabaseDonorBuilder 
    {
        public static Builder<Donor> New(string defaultHla) =>  Builder<Donor>.New
            .With(d => d.A_1, defaultHla)
            .With(d => d.A_2, defaultHla)
            .With(d => d.B_1, defaultHla)
            .With(d => d.B_2, defaultHla)
            .With(d => d.C_1, defaultHla)
            .With(d => d.C_2, defaultHla)
            .With(d => d.DPB1_1, defaultHla)
            .With(d => d.DPB1_2, defaultHla)
            .With(d => d.DRB1_1, defaultHla)
            .With(d => d.DRB1_2, defaultHla)
            .With(d => d.DQB1_1, defaultHla)
            .With(d => d.DQB1_2, defaultHla);

        public static Builder<Donor> WithPropValuesBasedOnId(int id, DatabaseDonorType donorType) => Builder<Donor>.New
            .With(d => d.AtlasId, id)
            .With(d => d.DonorType, donorType)
            .With(d => d.ExternalDonorCode, $"#{id}{nameof(Donor.ExternalDonorCode)}")
            .With(d => d.EthnicityCode, $"#{id}{nameof(Donor.EthnicityCode)}")
            .With(d => d.RegistryCode, $"#{id}{nameof(Donor.RegistryCode)}")
            .With(d => d.A_1, $"#{id}{nameof(Donor.A_1)}")
            .With(d => d.A_2, $"#{id}{nameof(Donor.A_2)}")
            .With(d => d.B_1, $"#{id}{nameof(Donor.B_1)}")
            .With(d => d.B_2, $"#{id}{nameof(Donor.B_2)}")
            .With(d => d.C_1, $"#{id}{nameof(Donor.C_1)}")
            .With(d => d.C_2, $"#{id}{nameof(Donor.C_2)}")
            .With(d => d.DPB1_1, $"#{id}{nameof(Donor.DPB1_1)}")
            .With(d => d.DPB1_2, $"#{id}{nameof(Donor.DPB1_2)}")
            .With(d => d.DQB1_1, $"#{id}{nameof(Donor.DQB1_1)}")
            .With(d => d.DQB1_2, $"#{id}{nameof(Donor.DQB1_2)}")
            .With(d => d.DRB1_1, $"#{id}{nameof(Donor.DRB1_1)}")
            .With(d => d.DRB1_2, $"#{id}{nameof(Donor.DRB1_2)}");
    }
}