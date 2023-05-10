using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.SubjectImport;

namespace Atlas.ManualTesting.Models
{
    internal static class ImportedSubjectExtensions
    {
        public static PhenotypeInfoTransfer<string> ToPhenotypeInfoTransfer(this ImportedSubject subject)
        {
            return new PhenotypeInfo<string>(
                    valueA_1: subject.A_1,
                    valueA_2: subject.A_2,
                    valueB_1: subject.B_1,
                    valueB_2: subject.B_2,
                    valueC_1: subject.C_1,
                    valueC_2: subject.C_2,
                    valueDqb1_1: subject.DQB1_1,
                    valueDqb1_2: subject.DQB1_2,
                    valueDrb1_1: subject.DRB1_1,
                    valueDrb1_2: subject.DRB1_2)
                .ToPhenotypeInfoTransfer();
        }
    }
}
