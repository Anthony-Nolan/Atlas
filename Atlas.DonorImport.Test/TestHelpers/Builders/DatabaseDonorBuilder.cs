using Atlas.DonorImport.Data.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
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
    }
}