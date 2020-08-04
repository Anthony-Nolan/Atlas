// ReSharper disable ClassNeverInstantiated.Global

namespace Atlas.Common.GeneticData.PhenotypeInfo.TransferModels
{
    /// <summary>
    /// A serialisable version of <see cref="PhenotypeInfo{T}"/>, for usage across serialisation boundaries.
    /// <see cref="PhenotypeInfo{T}"/> is immutable and contains helper methods and equality overrides, and should be used for all business logic needs.
    ///
    /// This class allows deserialisation via public setters, and should not be used anywhere but serialisation boundaries (e.g. HTTP APIs) 
    /// </summary>
    public class PhenotypeInfoTransfer<T> : LociInfoTransfer<LocusInfoTransfer<T>>
    {
    }

    /// <summary>
    /// A serialisable version of <see cref="LociInfo{T}"/>, for usage across serialisation boundaries.
    /// <see cref="LociInfo{T}"/> is immutable and contains helper methods and equality overrides, and should be used for all business logic needs.
    ///
    /// This class allows deserialisation via public setters, and should not be used anywhere but serialisation boundaries (e.g. HTTP APIs) 
    /// </summary>
    public class LociInfoTransfer<T>
    {
        public T A { get; set; }
        public T B { get; set; }
        public T C { get; set; }
        public T Dpb1 { get; set; }
        public T Dqb1 { get; set; }
        public T Drb1 { get; set; }
    }

    /// <summary>
    /// A serialisable version of <see cref="LocusInfo{T}"/>, for usage across serialisation boundaries.
    /// <see cref="LocusInfo{T}"/> is immutable and contains helper methods and equality overrides, and should be used for all business logic needs.
    ///
    /// This class allows deserialisation via public setters, and should not be used anywhere but serialisation boundaries (e.g. HTTP APIs) 
    /// </summary>
    public class LocusInfoTransfer<T>
    {
        public T Position1 { get; set; }
        public T Position2 { get; set; }
    }

    public static class Converters
    {
        public static PhenotypeInfo<T> ToPhenotypeInfo<T>(this PhenotypeInfoTransfer<T> phenotypeInfoTransfer) =>
            new PhenotypeInfo<T>
            {
                A = new LocusInfo<T>(phenotypeInfoTransfer.A.Position1, phenotypeInfoTransfer.A.Position2),
                B = new LocusInfo<T>(phenotypeInfoTransfer.B.Position1, phenotypeInfoTransfer.B.Position2),
                C = new LocusInfo<T>(phenotypeInfoTransfer.C.Position1, phenotypeInfoTransfer.C.Position2),
                Dpb1 = new LocusInfo<T>(phenotypeInfoTransfer.Dpb1.Position1, phenotypeInfoTransfer.Dpb1.Position2),
                Dqb1 = new LocusInfo<T>(phenotypeInfoTransfer.Dqb1.Position1, phenotypeInfoTransfer.Dqb1.Position2),
                Drb1 = new LocusInfo<T>(phenotypeInfoTransfer.Drb1.Position1, phenotypeInfoTransfer.Drb1.Position2),
            };

        public static PhenotypeInfoTransfer<T> ToPhenotypeInfoTransfer<T>(this PhenotypeInfo<T> phenotypeInfo) =>
            new PhenotypeInfoTransfer<T>
            {
                A = new LocusInfoTransfer<T>
                {
                    Position1 = phenotypeInfo.A.Position1, Position2 = phenotypeInfo.A.Position2
                },
                B = new LocusInfoTransfer<T>
                {
                    Position1 = phenotypeInfo.B.Position1, Position2 = phenotypeInfo.B.Position2
                },
                C = new LocusInfoTransfer<T>
                {
                    Position1 = phenotypeInfo.C.Position1, Position2 = phenotypeInfo.C.Position2
                },
                Dpb1 = new LocusInfoTransfer<T>
                {
                    Position1 = phenotypeInfo.Dpb1.Position1, Position2 = phenotypeInfo.Dpb1.Position2
                },
                Dqb1 = new LocusInfoTransfer<T>
                {
                    Position1 = phenotypeInfo.Dqb1.Position1, Position2 = phenotypeInfo.Dqb1.Position2
                },
                Drb1 = new LocusInfoTransfer<T>
                {
                    Position1 = phenotypeInfo.Drb1.Position1, Position2 = phenotypeInfo.Drb1.Position2
                },
            };

        public static LocusInfoTransfer<T> ToLocusInfoTransfer<T>(this LocusInfo<T> locusInfo) =>
            new LocusInfoTransfer<T>
            {
                Position1 = locusInfo.Position1,
                Position2 = locusInfo.Position2
            };
    }
}