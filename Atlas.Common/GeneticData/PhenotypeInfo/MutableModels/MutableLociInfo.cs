namespace Atlas.Common.GeneticData.PhenotypeInfo.MutableModels
{
    /// <summary>
    /// Intended for usage when using immutable hampers performance, i.e. when properties need to be mutated millions of times.
    ///
    /// In all other cases <see cref="LociInfo{T}"/> should be used.
    /// This class should *never* be used as a dictionary key. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MutableLociInfo<T>
    {
        public MutableLociInfo()
        {
        }

        public MutableLociInfo(T initialValue)
        {
            A = initialValue;
            B = initialValue;
            C = initialValue;
            Dpb1 = initialValue;
            Dqb1 = initialValue;
            Drb1 = initialValue;
        }

        public T A { get; set; }
        public T B { get; set; }
        public T C { get; set; }
        public T Dpb1 { get; set; }
        public T Dqb1 { get; set; }
        public T Drb1 { get; set; }
    }

    public static class Converters
    {
        public static LociInfo<T> ToLociInfo<T>(this MutableLociInfo<T> mutableLociInfo) => new LociInfo<T>(
            mutableLociInfo.A,
            mutableLociInfo.B,
            mutableLociInfo.C,
            mutableLociInfo.Dpb1,
            mutableLociInfo.Dqb1,
            mutableLociInfo.Drb1
        );
        
        public static MutableLociInfo<T> ToMutableLociInfo<T>(this LociInfo<T> lociInfo) => new MutableLociInfo<T>
        {
            A = lociInfo.A,
            B = lociInfo.B,
            C = lociInfo.C,
            Dpb1 = lociInfo.Dpb1,
            Dqb1 = lociInfo.Dqb1,
            Drb1 = lociInfo.Drb1
        };
    }
}