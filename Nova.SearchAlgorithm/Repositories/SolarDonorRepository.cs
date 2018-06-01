using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.Solar;

namespace Nova.SearchAlgorithm.Repositories
{
    public interface ISolarDonorRepository
    {
        IEnumerable<RawInputDonor> SomeDonors(int maxResults);
    }

    public class SolarDonorRepository : ISolarDonorRepository
    {
        private readonly ISolarConnectionFactory factory;

        public SolarDonorRepository(ISolarConnectionFactory factory)
        {
            this.factory = factory;
        }

        public IEnumerable<RawInputDonor> SomeDonors(int maxResults)
        {
            const string sql = @"SELECT 
                                   d.donor_id, birth_date, blood_group_type, rh_type, donor_type, donor_status_type, donor_state_type,
                                   reason_unavailable_type, unavailable_from_date, unavailable_to_date, reserved_patient_id,
                                   ethnic_group_type, gender_type, weight, cmv_antibody_type, cmv_tested_date, last_contact_date,
                                   upper_surname, prim_id, provided_marrow_flag,
                                   a_1_hla_name, a_1_nmdp_code, a_2_hla_name, a_2_nmdp_code,
                                   b_1_hla_name, b_1_nmdp_code, b_2_hla_name, b_2_nmdp_code,
                                   cw_1_hla_name, cw_1_nmdp_code, cw_2_hla_name, cw_2_nmdp_code,
                                   drb1_1_hla_name, drb1_1_nmdp_code, drb1_2_hla_name, drb1_2_nmdp_code,
                                   nvl2(drb3_1_hla_name, 'B3' || drb3_1_hla_name, null) as drb3_1_hla_name,
                                   nvl2(drb4_1_hla_name, 'B4' || drb4_1_hla_name, null) as drb4_1_hla_name,
                                   nvl2(drb5_1_hla_name, 'B5' || drb5_1_hla_name, null) as drb5_1_hla_name,
                                   nvl2(drb3_2_hla_name, 'B3' || drb3_2_hla_name, null) as drb3_2_hla_name,
                                   nvl2(drb4_2_hla_name, 'B4' || drb4_2_hla_name, null) as drb4_2_hla_name,
                                   nvl2(drb5_2_hla_name, 'B5' || drb5_2_hla_name, null) as drb5_2_hla_name,
                                   nvl2(drb3_1_nmdp_code, 'B3' || drb3_1_nmdp_code, null) as drb3_1_nmdp_code,
                                   nvl2(drb4_1_nmdp_code, 'B4' || drb4_1_nmdp_code, null) as drb4_1_nmdp_code,
                                   nvl2(drb5_1_nmdp_code, 'B5' || drb5_1_nmdp_code, null) as drb5_1_nmdp_code,
                                   nvl2(drb3_2_nmdp_code, 'B3' || drb3_2_nmdp_code, null) as drb3_2_nmdp_code,
                                   nvl2(drb4_2_nmdp_code, 'B4' || drb4_2_nmdp_code, null) as drb4_2_nmdp_code,
                                   nvl2(drb5_2_nmdp_code, 'B5' || drb5_2_nmdp_code, null) as drb5_2_nmdp_code,
                                   dqb1_1_hla_name, dqb1_1_nmdp_code, dqb1_2_hla_name, dqb1_2_nmdp_code,
                                   dqa1_1_hla_name, dqa1_1_nmdp_code, dqa1_2_hla_name, dqa1_2_nmdp_code,
                                   dpa1_1_hla_name, dpa1_1_nmdp_code, dpa1_2_hla_name, dpa1_2_nmdp_code,
                                   dpb1_1_hla_name, dpb1_1_nmdp_code, dpb1_2_hla_name, dpb1_2_nmdp_code
                               FROM
                                   dr_donors d, dr_definitive_hla_type_v dhv
                               WHERE
                                   ROWNUM <= :max_rows
                               AND
                                   d.donor_id = dhv.donor_patient_id
                               AND
                                   d.donor_status_type = 'Active'
                            ";

            using (var connection = factory.GetConnection())
            {
                var results = connection.Query(sql, new { max_rows = maxResults });
                return results.Select(RawDonorMap);
            }
        }

        private RawInputDonor RawDonorMap(dynamic result)
        {
            return new RawInputDonor
            {
                DonorId = result.DONOR_ID,
                RegistryCode = result.DONOR_TYPE,
                DonorType = "A",
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = result.A_1_HLA_NAME,
                    A_2 = result.A_2_HLA_NAME,
                    B_1 = result.B_1_HLA_NAME,
                    B_2 = result.B_2_HLA_NAME,
                    C_1 = result.C_1_HLA_NAME,
                    C_2 = result.C_2_HLA_NAME,
                    DQB1_1 = result.DQB1_1_HLA_NAME,
                    DQB1_2 = result.DQB1_2_HLA_NAME,
                    DRB1_1 = result.DRB1_1_HLA_NAME,
                    DRB1_2 = result.DRB1_2_HLA_NAME
                }
            };
        }
    }
}