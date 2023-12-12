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
            .With(d => d.ExternalDonorCode, $"#{id}ExtCode")
            .With(d => d.EthnicityCode, $"#{id}Eth")
            .With(d => d.RegistryCode, $"#{id}RC")
            .With(d => d.A_1, $"#{id}A_1")
            .With(d => d.A_2, $"#{id}A_2")
            .With(d => d.B_1, $"#{id}B_1")
            .With(d => d.B_2, $"#{id}B_2")
            .With(d => d.C_1, $"#{id}C_1")
            .With(d => d.C_2, $"#{id}C_2")
            .With(d => d.DPB1_1, $"#{id}DPB1_1")
            .With(d => d.DPB1_2, $"#{id}DPB1_2")
            .With(d => d.DQB1_1, $"#{id}DQB1_1")
            .With(d => d.DQB1_2, $"#{id}DQB1_2")
            .With(d => d.DRB1_1, $"#{id}DRB1_1")
            .With(d => d.DRB1_2, $"#{id}DRB1_2");
    }
}