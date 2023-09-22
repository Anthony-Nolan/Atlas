using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using LochNessBuilder;
using System;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels
{
    [Builder]
    public static class DonorBuilder 
    {
        private const string DefaultHlaName = "hla-name";
        private const DonorType DefaultDonorType = DonorType.Adult;

        public static Builder<Donor> New => Builder<Donor>.New
            .With(d => d.AtlasDonorId, 0)
            .With(d => d.A_1, DefaultHlaName)
            .With(d => d.A_2, DefaultHlaName)
            .With(d => d.B_1, DefaultHlaName)
            .With(d => d.B_2, DefaultHlaName)
            .With(d => d.DRB1_1, DefaultHlaName)
            .With(d => d.DRB1_2, DefaultHlaName)
            .With(d => d.DonorType, DefaultDonorType)
            .With(d => d.ExternalDonorCode, Guid.NewGuid().ToString());

        public static Builder<Donor> WithDefaultValidHla(this Builder<Donor> builder, string defaultHla) => builder
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
    }
}