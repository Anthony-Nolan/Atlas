using Atlas.DonorImport.Data.Models;

namespace Atlas.DonorImport.Data.Repositories
{
    public abstract class DonorRepositoryBase
    {
        protected readonly string ConnectionString;

        // The order of these matters when setting up the datatable - if re-ordering, also re-order datatable contents
        protected readonly string[] DonorInsertDataTableColumnNames = {
            nameof(Donor.Id),
            nameof(Donor.DonorId),
            nameof(Donor.DonorType),
            nameof(Donor.EthnicityCode),
            nameof(Donor.RegistryCode),
            nameof(Donor.A_1),
            nameof(Donor.A_2),
            nameof(Donor.B_1),
            nameof(Donor.B_2),
            nameof(Donor.C_1),
            nameof(Donor.C_2),
            nameof(Donor.DPB1_1),
            nameof(Donor.DPB1_2),
            nameof(Donor.DQB1_1),
            nameof(Donor.DQB1_2),
            nameof(Donor.DRB1_1),
            nameof(Donor.DRB1_2),
            nameof(Donor.Hash)
        };

        protected DonorRepositoryBase(string connectionString)
        {
            this.ConnectionString = connectionString;
        }
    }
}