using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using LochNessBuilder;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    [Builder]
    internal class LocusInfoBuilder<T>
    {
        private LocusInfo<T> locusInfo;

        public LocusInfoBuilder()
        {
            locusInfo = new LocusInfo<T>();
        }

        public LocusInfoBuilder(T initialValue)
        {
            locusInfo = new LocusInfo<T>(initialValue);
        }

        public LocusInfoBuilder(T initialValue1, T initialValue2)
        {
            locusInfo = new LocusInfo<T>(initialValue1, initialValue2);
        }

        public LocusInfoBuilder<T> WithDataAt(LocusPosition position, T data)
        {
            locusInfo = locusInfo.SetAtPosition(position, data);
            return this;
        }

        public LocusInfo<T> Build()
        {
            return locusInfo;
        }
    }
}