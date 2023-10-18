using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public class PhenotypeInfoBuilder<T>
    {
        // used to reset Builder to starting values
        private readonly PhenotypeInfo<T> initialPhenotypeInfo;

        private PhenotypeInfo<T> phenotypeInfo;

        public PhenotypeInfoBuilder(PhenotypeInfo<T> initialValues = null)
        {
            phenotypeInfo = initialValues == null
                ? new PhenotypeInfo<T>(new LocusInfo<T>((T)default))
                : new PhenotypeInfo<T>(initialValues);

            // clone the initial version of phenotype info
            initialPhenotypeInfo = new PhenotypeInfo<T>(phenotypeInfo);
        }

        public PhenotypeInfoBuilder(T initialValue) : this(new PhenotypeInfo<T>(initialValue))
        {
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

        public void ResetToStartingValues()
        {
            phenotypeInfo = new PhenotypeInfo<T>(initialPhenotypeInfo);
        }
    }
}