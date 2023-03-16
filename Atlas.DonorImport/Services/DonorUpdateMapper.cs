using Atlas.Common.Public.Models.GeneticData;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Models;
using Atlas.DonorImport.Models.Mapping;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorUpdateMapper
    {
        Donor MapToDatabaseDonor(DonorUpdate fileUpdate, string fileLocation);
    }

    internal class DonorUpdateMapper : IDonorUpdateMapper
    {
        private readonly IImportedLocusInterpreter locusInterpreter;

        public DonorUpdateMapper(IImportedLocusInterpreter locusInterpreter)
        {
            this.locusInterpreter = locusInterpreter;
        }
        
        public Donor MapToDatabaseDonor(DonorUpdate fileUpdate, string fileLocation)
        {
            Dictionary<string, string> CreateLogContext(Locus locus) =>
                new()
                {
                    { "ImportFile", fileLocation },
                    { "DonorCode", fileUpdate.RecordId },
                    { "Locus", locus.ToString() }
                };

            var interpretedA = locusInterpreter.Interpret(fileUpdate.Hla.A, CreateLogContext(Locus.A));
            var interpretedB = locusInterpreter.Interpret(fileUpdate.Hla.B, CreateLogContext(Locus.B));
            var interpretedC = locusInterpreter.Interpret(fileUpdate.Hla.C, CreateLogContext(Locus.C));
            var interpretedDpb1 = locusInterpreter.Interpret(fileUpdate.Hla.DPB1, CreateLogContext(Locus.Dpb1));
            var interpretedDqb1 = locusInterpreter.Interpret(fileUpdate.Hla.DQB1, CreateLogContext(Locus.Dqb1));
            var interpretedDrb1 = locusInterpreter.Interpret(fileUpdate.Hla.DRB1, CreateLogContext(Locus.Drb1));

            var storedFileLocation = LeftTruncateTo256(fileLocation);

            var donor = new Donor
            {
                ExternalDonorCode = fileUpdate.RecordId,
                UpdateFile = storedFileLocation,
                LastUpdated = DateTimeOffset.UtcNow,
                DonorType = fileUpdate.DonorType.ToDatabaseType(),
                EthnicityCode = fileUpdate.Ethnicity,
                RegistryCode = fileUpdate.RegistryCode,
                A_1 = interpretedA.Position1,
                A_2 = interpretedA.Position2,
                B_1 = interpretedB.Position1,
                B_2 = interpretedB.Position2,
                C_1 = interpretedC.Position1,
                C_2 = interpretedC.Position2,
                DPB1_1 = interpretedDpb1.Position1,
                DPB1_2 = interpretedDpb1.Position2,
                DQB1_1 = interpretedDqb1.Position1,
                DQB1_2 = interpretedDqb1.Position2,
                DRB1_1 = interpretedDrb1.Position1,
                DRB1_2 = interpretedDrb1.Position2,
            };
            donor.Hash = donor.CalculateHash();
            return donor;
        }

        /// <summary>
        /// The UpdateFile field is a varchar(256), so we need to ensure that the string we try to store there is no more than 256 characters.
        /// If the actual file name is > 256, then there's not much we can do.
        /// But realistically if we *are* over 256, then it's more likely because the container is nested.
        /// In that case the *end* of the path is far more interesting than the start of it.
        /// So we should truncate from the left, rather than the right.
        /// </summary>
        private static string LeftTruncateTo256(string fileLocation) => 
            fileLocation.Length > 256
                ? new string(fileLocation.TakeLast(256).ToArray())
                : fileLocation;
    }
}
