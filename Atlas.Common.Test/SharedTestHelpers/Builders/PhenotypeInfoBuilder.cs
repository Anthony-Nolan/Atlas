using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public class PhenotypeInfoBuilder<T>
    {
        private PhenotypeInfo<T> phenotypeInfo;

        public PhenotypeInfoBuilder()
        {
            phenotypeInfo = new PhenotypeInfo<T>(new LocusInfo<T>((T) default));
        }

        public PhenotypeInfoBuilder(T initialValue)
        {
            phenotypeInfo = new PhenotypeInfo<T>(initialValue);
        }

        public PhenotypeInfoBuilder(PhenotypeInfo<T> initialValues)
        {
            phenotypeInfo = new PhenotypeInfo<T>(initialValues);
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, LocusPosition position, T value)
        {
            phenotypeInfo = phenotypeInfo.SetPosition(locus, position, value);
            return this;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, T value)
        {
            phenotypeInfo = phenotypeInfo.SetLocus(locus, value);
            return this;
        }


        public PhenotypeInfoBuilder<T> WithDataAtLoci(T value, params Locus[] loci)
        {
            foreach (var locus in loci)
            {
                phenotypeInfo = phenotypeInfo.SetLocus(locus, value);
            }

            return this;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, T value1, T value2)
        {
            phenotypeInfo = phenotypeInfo
                .SetPosition(locus, LocusPosition.One, value1)
                .SetPosition(locus, LocusPosition.Two, value2);
            return this;
        }

        public PhenotypeInfoBuilder<T> WithDataAt(Locus locus, LocusInfo<T> value)
        {
            phenotypeInfo = phenotypeInfo.SetLocus(locus, value).ToPhenotypeInfo();
            return this;
        }

        public PhenotypeInfo<T> Build()
        {
            return phenotypeInfo;
        }
    }
}