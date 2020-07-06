using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;
using Microsoft.Azure.Documents.Spatial;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public class PhenotypeInfoBuilder<T>
    {
        private readonly PhenotypeInfo<T> phenotypeInfo;

        public PhenotypeInfoBuilder()
        {
            phenotypeInfo = new PhenotypeInfo<T>
            {
                A = new LocusInfo<T>(default),
                B = new LocusInfo<T>(default),
                C = new LocusInfo<T>(default),
                Dpb1 = new LocusInfo<T>(default),
                Dqb1 = new LocusInfo<T>(default),
                Drb1 = new LocusInfo<T>(default),
            };
        }

        public PhenotypeInfoBuilder(PhenotypeInfo<T> initialValues)
        {
            phenotypeInfo = initialValues;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, LocusPosition position, T value)
        {
            phenotypeInfo.SetPosition(locus, position, value);
            return this;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, T value)
        {
            phenotypeInfo.SetLocus(locus, value);
            return this;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, T value1, T value2)
        {
            phenotypeInfo.SetPosition(locus, LocusPosition.One, value1);
            phenotypeInfo.SetPosition(locus, LocusPosition.Two, value2);
            return this;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, LocusInfo<T> value)
        {
            phenotypeInfo.SetLocus(locus, value);
            return this;
        }

        public PhenotypeInfo<T> Build()
        {
            return phenotypeInfo;
        }
    }
}