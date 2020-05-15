using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable MemberCanBePrivate.Global

// TODO: ATLAS-121: Merge with Atlas version of PhenotypeInfo
namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// Data type to hold one instance of T for each of the supported HLA loci and each type position within.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each loci position.</typeparam>
    public class PhenotypeInfo<T> : LociInfo<LocusInfo<T>>
    {
        /// <summary>
        /// Creates a new PhenotypeInfo with no inner values set.
        /// </summary>
        public PhenotypeInfo()
        {
            Initialise();
        }

        /// <summary>
        /// Creates a new PhenotypeInfo using the provided LociInfo.
        /// </summary>
        public PhenotypeInfo(LociInfo<LocusInfo<T>> source)
        {
            A = source.A;
            B = source.B;
            C = source.C;
            Dpa1 = source.Dpa1;
            Dpb1 = source.Dpb1;
            Dqa1 = source.Dqa1;
            Dqb1 = source.Dqb1;
            Drb1 = source.Drb1;
            Drb3 = source.Drb3;
            Drb4 = source.Drb4;
            Drb5 = source.Drb5;
        }

        /// <summary>
        /// Creates a new PhenotypeInfo with all inner values set to the same starting value.
        /// </summary>
        /// <param name="initialValue">The initial value all inner locus position values should be given.</param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1642:ConstructorSummaryDocumentationMustBeginWithStandardText", Justification = "Disabled.")]
        public PhenotypeInfo(T initialValue)
        {
            Initialise();
            A.Position1 = initialValue;
            A.Position2 = initialValue;
            B.Position1 = initialValue;
            B.Position2 = initialValue;
            C.Position1 = initialValue;
            C.Position2 = initialValue;
            Dpa1.Position1 = initialValue;
            Dpa1.Position2 = initialValue;
            Dpb1.Position1 = initialValue;
            Dpb1.Position2 = initialValue;
            Dqa1.Position1 = initialValue;
            Dqa1.Position2 = initialValue;
            Dqb1.Position1 = initialValue;
            Dqb1.Position2 = initialValue;
            Drb1.Position1 = initialValue;
            Drb1.Position2 = initialValue;
            Drb3.Position1 = initialValue;
            Drb3.Position2 = initialValue;
            Drb4.Position1 = initialValue;
            Drb4.Position2 = initialValue;
            Drb5.Position1 = initialValue;
            Drb5.Position2 = initialValue;
        }

        public PhenotypeInfo<R> Map<R>(Func<LocusType, LocusPosition, T, R> mapping)
        {
            return new PhenotypeInfo<R>(Map((locusType, locusInfo) => new LocusInfo<R>()
            {
                Position1 = mapping(locusType, LocusPosition.Position1, locusInfo.Position1),
                Position2 = mapping(locusType, LocusPosition.Position2, locusInfo.Position2)
            }));
        }

        public PhenotypeInfo<R> Map<R>(Func<LocusType, T, R> mapping)
        {
            return Map((locusType, position, locusInfo) => mapping(locusType, locusInfo));
        }

        public PhenotypeInfo<R> Map<R>(Func<T, R> mapping)
        {
            return Map((locusType, position, locusInfo) => mapping(locusInfo));
        }

        public void SetAtPosition(LocusType locus, LocusPosition position, T value)
        {
            GetLocus(locus).SetAtPosition(position, value);
        }

        public T GetAtPosition(LocusType locus, LocusPosition position)
        {
            return GetLocus(locus).GetAtPosition(position);
        }

        public new IEnumerable<T> ToEnumerable()
        {
            return new List<T>
            {
                A.Position1,
                A.Position2,
                B.Position1,
                B.Position2,
                C.Position1,
                C.Position2,
                Dpa1.Position1,
                Dpa1.Position2,
                Dpb1.Position1,
                Dpb1.Position2,
                Dqa1.Position1,
                Dqa1.Position2,
                Dqb1.Position1,
                Dqb1.Position2,
                Drb1.Position1,
                Drb1.Position2,
                Drb3.Position1,
                Drb3.Position2,
                Drb4.Position1,
                Drb4.Position2,
                Drb5.Position1,
                Drb5.Position2,
            };
        }

        private void Initialise()
        {
            A = new LocusInfo<T>();
            B = new LocusInfo<T>();
            Dpa1 = new LocusInfo<T>();
            Dpb1 = new LocusInfo<T>();
            Dqa1 = new LocusInfo<T>();
            Dqb1 = new LocusInfo<T>();
            Drb1 = new LocusInfo<T>();
            Drb3 = new LocusInfo<T>();
            Drb4 = new LocusInfo<T>();
            Drb5 = new LocusInfo<T>();
            C = new LocusInfo<T>();
        }
    }
}