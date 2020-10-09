using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class GenotypeSimulantsInfoBuilder
    {
        public static Builder<GenotypeSimulantsInfo> New => Builder<GenotypeSimulantsInfo>.New;

        public static Builder<GenotypeSimulantsInfo> WithEmptySimulantsInfo => New
            .WithPatients(new List<Simulant>())
            .WithDonors(new List<Simulant>());

        public static Builder<GenotypeSimulantsInfo> WithPatient(this Builder<GenotypeSimulantsInfo> builder, Simulant simulant)
        {
            return builder.WithPatients(new[] { simulant });
        }

        public static Builder<GenotypeSimulantsInfo> WithDonor(this Builder<GenotypeSimulantsInfo> builder, Simulant simulant)
        {
            return builder.WithDonors(new[] { simulant });
        }

        private static Builder<GenotypeSimulantsInfo> WithPatients(this Builder<GenotypeSimulantsInfo> builder, IReadOnlyCollection<Simulant> simulants)
        {
            return builder.With(x => x.Patients, BuildSimulantsInfo(simulants));
        }

        private static Builder<GenotypeSimulantsInfo> WithDonors(this Builder<GenotypeSimulantsInfo> builder, IReadOnlyCollection<Simulant> simulants)
        {
            return builder.With(x => x.Donors, BuildSimulantsInfo(simulants));
        }

        private static SimulantsInfo BuildSimulantsInfo(IReadOnlyCollection<Simulant> simulants)
        {
            return new SimulantsInfo
            {
                Hla = simulants,
                Ids = simulants.Select(s => s.Id).ToList()
            };
        }
    }
}
