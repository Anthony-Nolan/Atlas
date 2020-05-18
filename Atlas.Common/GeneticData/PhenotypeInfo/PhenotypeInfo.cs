using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

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
            Dpb1 = source.Dpb1;
            Dqb1 = source.Dqb1;
            Drb1 = source.Drb1;
        }

        /// <summary>
        /// Creates a new PhenotypeInfo with all inner values set to the same starting value.
        /// </summary>
        /// <param name="initialValue">The initial value all inner locus position values should be given.</param>
        public PhenotypeInfo(T initialValue)
        {
            Initialise();
            A.Position1 = initialValue;
            A.Position2 = initialValue;
            B.Position1 = initialValue;
            B.Position2 = initialValue;
            C.Position1 = initialValue;
            C.Position2 = initialValue;
            Dpb1.Position1 = initialValue;
            Dpb1.Position2 = initialValue;
            Dqb1.Position1 = initialValue;
            Dqb1.Position2 = initialValue;
            Drb1.Position1 = initialValue;
            Drb1.Position2 = initialValue;
        }

        public PhenotypeInfo<R> Map<R>(Func<Locus, LocusPosition, T, R> mapping)
        {
            return new PhenotypeInfo<R>(Map((locusType, locusInfo) => new LocusInfo<R>()
            {
                Position1 = mapping(locusType, LocusPosition.Position1, locusInfo.Position1),
                Position2 = mapping(locusType, LocusPosition.Position2, locusInfo.Position2)
            }));
        }

        public PhenotypeInfo<R> Map<R>(Func<Locus, T, R> mapping)
        {
            return Map((locusType, position, locusInfo) => mapping(locusType, locusInfo));
        }

        public PhenotypeInfo<R> Map<R>(Func<T, R> mapping)
        {
            return Map((locusType, position, locusInfo) => mapping(locusInfo));
        }
        
        // TODO: Use locusInfo
        public PhenotypeInfo<R> MapByLocus<R>(Func<Locus, T, T, Tuple<R, R>> mapping)
        {
            var a = mapping(Locus.A, A.Position1, A.Position2);
            var b = mapping(Locus.B, B.Position1, B.Position2);
            var c = mapping(Locus.C, C.Position1, C.Position2);
            var dpb1 = mapping(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2);
            var dqb1 = mapping(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2);
            var drb1 = mapping(Locus.Drb1, Drb1.Position1, Drb1.Position2);

            return new PhenotypeInfo<R>
            {
                A =
                {
                    Position1 = a.Item1,
                    Position2 = a.Item2,
                },
                B =
                {
                    Position1 = b.Item1,
                    Position2 = b.Item2,
                },
                C =
                {
                    Position1 = c.Item1,
                    Position2 = c.Item2,
                },
                Dpb1 =
                {
                    Position1 = dpb1.Item1,
                    Position2 = dpb1.Item2,
                },
                Dqb1 =
                {
                    Position1 = dqb1.Item1,
                    Position2 = dqb1.Item2,
                },
                Drb1 =
                {
                    Position1 = drb1.Item1,
                    Position2 = drb1.Item2,
                }
            };
        }

        public void SetPosition(Locus locus, LocusPosition position, T value)
        {
            GetLocus(locus).SetAtPosition(position, value);
        }

        public T GetPosition(Locus locus, LocusPosition position)
        {
            return GetLocus(locus).GetAtPosition(position);
        }

        public void SetLocus(Locus locus, T value)
        {
            SetPosition(locus, LocusPosition.Position1, value);
            SetPosition(locus, LocusPosition.Position2, value);
        }
        
        public void EachPosition(Action<Locus, LocusPosition, T> action)
        {
            action(Locus.A, LocusPosition.Position1, A.Position1);
            action(Locus.A, LocusPosition.Position2, A.Position2);
            action(Locus.B, LocusPosition.Position1, B.Position1);
            action(Locus.B, LocusPosition.Position2, B.Position2);
            action(Locus.C, LocusPosition.Position1, C.Position1);
            action(Locus.C, LocusPosition.Position2, C.Position2);
            action(Locus.Dpb1, LocusPosition.Position1, Dpb1.Position1);
            action(Locus.Dpb1, LocusPosition.Position2, Dpb1.Position2);
            action(Locus.Dqb1, LocusPosition.Position1, Dqb1.Position1);
            action(Locus.Dqb1, LocusPosition.Position2, Dqb1.Position2);
            action(Locus.Drb1, LocusPosition.Position1, Drb1.Position1);
            action(Locus.Drb1, LocusPosition.Position2, Drb1.Position2);
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
                Dpb1.Position1,
                Dpb1.Position2,
                Dqb1.Position1,
                Dqb1.Position2,
                Drb1.Position1,
                Drb1.Position2,
            };
        }

        private void Initialise()
        {
            A = new LocusInfo<T>();
            B = new LocusInfo<T>();
            Dpb1 = new LocusInfo<T>();
            Dqb1 = new LocusInfo<T>();
            Drb1 = new LocusInfo<T>();
            C = new LocusInfo<T>();
        }

        public async Task WhenAllLoci(Func<Locus, T, T, Task> action)
        {
            await Task.WhenAll(
                action(Locus.A, A.Position1, A.Position2),
                action(Locus.B, B.Position1, B.Position2),
                action(Locus.C, C.Position1, C.Position2),
                action(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2),
                action(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2),
                action(Locus.Drb1, Drb1.Position1, Drb1.Position2));
        }
    }
}