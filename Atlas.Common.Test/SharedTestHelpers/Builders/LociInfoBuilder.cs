using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    public class LociInfoBuilder<T>
    {
        private readonly LociInfo<T> lociInfo;

        public LociInfoBuilder()
        {
            lociInfo = new LociInfo<T>(default);
        }

        public LociInfoBuilder(LociInfo<T> initialValues)
        {
            lociInfo = initialValues;
        }

        public LociInfoBuilder<T> WithDataAt(Locus locus, T value)
        {
            lociInfo.SetLocus(locus, value);
            return this;
        }

        public LociInfo<T> Build()
        {
            return lociInfo;
        }
    }
}