using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public class LociInfoBuilder<T>
    {
        private LociInfo<T> lociInfo;

        public LociInfoBuilder()
        {
            lociInfo = new LociInfo<T>((T) default);
        }

        public LociInfoBuilder(LociInfo<T> initialValues)
        {
            lociInfo = initialValues;
        }
        
        public LociInfoBuilder(T initialValue)
        {
            lociInfo = new LociInfo<T>(initialValue);
        }

        public LociInfoBuilder<T> WithDataAt(Locus locus, T value)
        {
            lociInfo = lociInfo.SetLocus(locus, value);
            return this;
        }

        public LociInfoBuilder<T> WithDataAt(T value, params Locus[] loci)
        {
            lociInfo = lociInfo.SetLoci(value, loci);
            return this;
        }

        public LociInfo<T> Build()
        {
            return lociInfo;
        }
    }
}