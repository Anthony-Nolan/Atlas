using System.Collections.Generic;
using System.Linq;

namespace Atlas.Utils.Core.Models
{
    // TODO NOVA-1978: Implement this class as PhenotypeInfo<Antigen>
    public class Hla
    {
        public Antigen A_1 { get; set; } = new Antigen { Locus = LocusType.A };
        public Antigen A_2 { get; set; } = new Antigen { Locus = LocusType.A };
        public Antigen B_1 { get; set; } = new Antigen { Locus = LocusType.B };
        public Antigen B_2 { get; set; } = new Antigen { Locus = LocusType.B };
        public Antigen C_1 { get; set; } = new Antigen { Locus = LocusType.C };
        public Antigen C_2 { get; set; } = new Antigen { Locus = LocusType.C };
        public Antigen Drb1_1 { get; set; } = new Antigen { Locus = LocusType.Drb1 };
        public Antigen Drb1_2 { get; set; } = new Antigen { Locus = LocusType.Drb1 };
        public Antigen Drb3_1 { get; set; } = new Antigen { Locus = LocusType.Drb3 };
        public Antigen Drb3_2 { get; set; } = new Antigen { Locus = LocusType.Drb3 };
        public Antigen Drb4_1 { get; set; } = new Antigen { Locus = LocusType.Drb4 };
        public Antigen Drb4_2 { get; set; } = new Antigen { Locus = LocusType.Drb4 };
        public Antigen Drb5_1 { get; set; } = new Antigen { Locus = LocusType.Drb5 };
        public Antigen Drb5_2 { get; set; } = new Antigen { Locus = LocusType.Drb5 };
        public Antigen Dqb1_1 { get; set; } = new Antigen { Locus = LocusType.Dqb1 };
        public Antigen Dqb1_2 { get; set; } = new Antigen { Locus = LocusType.Dqb1 };
        public Antigen Dqa1_1 { get; set; } = new Antigen { Locus = LocusType.Dqa1 };
        public Antigen Dqa1_2 { get; set; } = new Antigen { Locus = LocusType.Dqa1 };
        public Antigen Dpa1_1 { get; set; } = new Antigen { Locus = LocusType.Dpa1 };
        public Antigen Dpa1_2 { get; set; } = new Antigen { Locus = LocusType.Dpa1 };
        public Antigen Dpb1_1 { get; set; } = new Antigen { Locus = LocusType.Dpb1 };
        public Antigen Dpb1_2 { get; set; } = new Antigen { Locus = LocusType.Dpb1 };

        public void PopulateDetails(Dictionary<int, Antigen> antigens)
        {
            A_1 = GetDetailsForAntigen(A_1, antigens);
            A_2 = GetDetailsForAntigen(A_2, antigens);
            B_1 = GetDetailsForAntigen(B_1, antigens);
            B_2 = GetDetailsForAntigen(B_2, antigens);
            C_1 = GetDetailsForAntigen(C_1, antigens);
            C_2 = GetDetailsForAntigen(C_2, antigens);
            Dpa1_1 = GetDetailsForAntigen(Dpa1_1, antigens);
            Dpa1_2 = GetDetailsForAntigen(Dpa1_2, antigens);
            Dpb1_1 = GetDetailsForAntigen(Dpb1_1, antigens);
            Dpb1_2 = GetDetailsForAntigen(Dpb1_2, antigens);
            Dqa1_1 = GetDetailsForAntigen(Dqa1_1, antigens);
            Dqa1_2 = GetDetailsForAntigen(Dqa1_2, antigens);
            Dqb1_1 = GetDetailsForAntigen(Dqb1_1, antigens);
            Dqb1_2 = GetDetailsForAntigen(Dqb1_2, antigens);
            Drb1_1 = GetDetailsForAntigen(Drb1_1, antigens);
            Drb1_2 = GetDetailsForAntigen(Drb1_2, antigens);
            Drb3_1 = GetDetailsForAntigen(Drb3_1, antigens);
            Drb3_2 = GetDetailsForAntigen(Drb3_2, antigens);
            Drb4_1 = GetDetailsForAntigen(Drb4_1, antigens);
            Drb4_2 = GetDetailsForAntigen(Drb4_2, antigens);
            Drb5_1 = GetDetailsForAntigen(Drb5_1, antigens);
            Drb5_2 = GetDetailsForAntigen(Drb5_2, antigens);
        }

        public IEnumerable<int> GetAllIds()
        {
            return GetAllAntigens()
                .Where(antigen => antigen.Id.HasValue)
                .Select(antigen => antigen.Id.Value);
        }

        public IEnumerable<Antigen> GetAllAntigens()
        {
            yield return A_1;
            yield return A_2;
            yield return B_1;
            yield return B_2;
            yield return C_1;
            yield return C_2;
            yield return Dpa1_1;
            yield return Dpa1_2;
            yield return Dpb1_1;
            yield return Dpb1_2;
            yield return Dqa1_1;
            yield return Dqa1_2;
            yield return Dqb1_1;
            yield return Dqb1_2;
            yield return Drb1_1;
            yield return Drb1_2;
            yield return Drb3_1;
            yield return Drb3_2;
            yield return Drb4_1;
            yield return Drb4_2;
            yield return Drb5_1;
            yield return Drb5_2;
        }

        private static Antigen GetDetailsForAntigen(Antigen antigen, Dictionary<int, Antigen> antigensById)
        {
            return antigen.Id.HasValue ? antigensById[antigen.Id.Value] : antigen;
        }
    }
}