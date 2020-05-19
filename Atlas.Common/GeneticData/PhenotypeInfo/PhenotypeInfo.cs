using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global
namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// Data type to hold one instance of T for each of the supported HLA loci and each type position within.
    /// 
    /// <see cref="LocusInfo{T}"/> is a single Locus' information - with a T at each position.
    /// A <see cref="LociInfo{T}"/> has a T at each locus.
    /// A <see cref="PhenotypeInfo{T}"/> is a special case of <see cref="LociInfo{T}"/>, where T = LocusInfo.
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
            A = new LocusInfo<T>(source.A.Position1, source.A.Position2);
            B = new LocusInfo<T>(source.B.Position1, source.B.Position2);
            C = new LocusInfo<T>(source.C.Position1, source.C.Position2);
            Dpb1 = new LocusInfo<T>(source.Dpb1.Position1, source.Dpb1.Position2);
            Dqb1 = new LocusInfo<T>(source.Dqb1.Position1, source.Dqb1.Position2);
            Drb1 = new LocusInfo<T>(source.Drb1.Position1, source.Drb1.Position2);
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
            return new PhenotypeInfo<R>(Map((locusType, locusInfo) => new LocusInfo<R>(
                mapping(locusType, LocusPosition.One, locusInfo.Position1),
                mapping(locusType, LocusPosition.Two, locusInfo.Position2)
            )));
        }

        public PhenotypeInfo<R> Map<R>(Func<Locus, T, R> mapping)
        {
            return Map((locusType, position, locusInfo) => mapping(locusType, locusInfo));
        }

        public PhenotypeInfo<R> Map<R>(Func<T, R> mapping)
        {
            return Map((locusType, position, locusInfo) => mapping(locusInfo));
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public async Task<PhenotypeInfo<R>> MapAsync<R>(Func<Locus, LocusPosition, T, Task<R>> mapping)
        {
            var a_1 = mapping(Locus.A, LocusPosition.One, A.Position1);
            var a_2 = mapping(Locus.A, LocusPosition.Two, A.Position2);
            var b_1 = mapping(Locus.B, LocusPosition.One, B.Position1);
            var b_2 = mapping(Locus.B, LocusPosition.Two, B.Position2);
            var c_1 = mapping(Locus.C, LocusPosition.One, C.Position1);
            var c_2 = mapping(Locus.C, LocusPosition.Two, C.Position2);
            var dpb1_1 = mapping(Locus.Dpb1, LocusPosition.One, Dpb1.Position1);
            var dpb1_2 = mapping(Locus.Dpb1, LocusPosition.Two, Dpb1.Position2);
            var dqb1_1 = mapping(Locus.Dqb1, LocusPosition.One, Dqb1.Position1);
            var dqb1_2 = mapping(Locus.Dqb1, LocusPosition.Two, Dqb1.Position2);
            var drb1_1 = mapping(Locus.Drb1, LocusPosition.One, Drb1.Position1);
            var drb1_2 = mapping(Locus.Drb1, LocusPosition.Two, Drb1.Position2);

            await Task.WhenAll(a_1, a_2, b_1, b_2, c_1, c_2, dpb1_1, dpb1_2, dqb1_1, dqb1_2, drb1_1, drb1_2);

            return new PhenotypeInfo<R>
            {
                A = new LocusInfo<R>(a_1.Result, a_2.Result),
                B = new LocusInfo<R>(b_1.Result, b_2.Result),
                C = new LocusInfo<R>(c_1.Result, c_2.Result),
                Dpb1 = new LocusInfo<R>(dpb1_1.Result, dpb1_2.Result),
                Dqb1 = new LocusInfo<R>(dqb1_1.Result, dqb1_2.Result),
                Drb1 = new LocusInfo<R>(drb1_1.Result, drb1_2.Result),
            };
        }

        // TODO: Use locusInfo
        public PhenotypeInfo<R> MapByLocus<R>(Func<Locus, T, T, LocusInfo<R>> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A = mapping(Locus.A, A.Position1, A.Position2),
                B = mapping(Locus.B, B.Position1, B.Position2),
                C = mapping(Locus.C, C.Position1, C.Position2),
                Dpb1 = mapping(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2),
                Dqb1 = mapping(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2),
                Drb1 = mapping(Locus.Drb1, Drb1.Position1, Drb1.Position2)
            };
        }

        public async Task<PhenotypeInfo<R>> MapByLocusAsync<R>(Func<Locus, T, T, Task<LocusInfo<R>>> action)
        {
            var a = action(Locus.A, A.Position1, A.Position2);
            var b = action(Locus.B, B.Position1, B.Position2);
            var c = action(Locus.C, C.Position1, C.Position2);
            var dpb1 = action(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2);
            var dqb1 = action(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2);
            var drb1 = action(Locus.Drb1, Drb1.Position1, Drb1.Position2);

            await Task.WhenAll(a, b, c, dpb1, dqb1, drb1);

            return new PhenotypeInfo<R>
            {
                A =
                {
                    Position1 = a.Result.Position1,
                    Position2 = a.Result.Position2,
                },
                B =
                {
                    Position1 = b.Result.Position1,
                    Position2 = b.Result.Position2,
                },
                C =
                {
                    Position1 = c.Result.Position1,
                    Position2 = c.Result.Position2,
                },
                Dpb1 =
                {
                    Position1 = dpb1.Result.Position1,
                    Position2 = dpb1.Result.Position2,
                },
                Dqb1 =
                {
                    Position1 = dqb1.Result.Position1,
                    Position2 = dqb1.Result.Position2,
                },
                Drb1 =
                {
                    Position1 = drb1.Result.Position1,
                    Position2 = drb1.Result.Position2
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
            SetPosition(locus, LocusPosition.One, value);
            SetPosition(locus, LocusPosition.Two, value);
        }

        public void EachLocus(Action<Locus, T, T> action)
        {
            action(Locus.A, A.Position1, A.Position2);
            action(Locus.B, B.Position1, B.Position2);
            action(Locus.C, C.Position1, C.Position2);
            action(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2);
            action(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2);
            action(Locus.Drb1, Drb1.Position1, Drb1.Position2);
        }

        public async Task EachLocusAsync(Func<Locus, T, T, Task> action)
        {
            await Task.WhenAll(
                action(Locus.A, A.Position1, A.Position2),
                action(Locus.B, B.Position1, B.Position2),
                action(Locus.C, C.Position1, C.Position2),
                action(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2),
                action(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2),
                action(Locus.Drb1, Drb1.Position1, Drb1.Position2));
        }

        public void EachPosition(Action<Locus, LocusPosition, T> action)
        {
            action(Locus.A, LocusPosition.One, A.Position1);
            action(Locus.A, LocusPosition.Two, A.Position2);
            action(Locus.B, LocusPosition.One, B.Position1);
            action(Locus.B, LocusPosition.Two, B.Position2);
            action(Locus.C, LocusPosition.One, C.Position1);
            action(Locus.C, LocusPosition.Two, C.Position2);
            action(Locus.Dpb1, LocusPosition.One, Dpb1.Position1);
            action(Locus.Dpb1, LocusPosition.Two, Dpb1.Position2);
            action(Locus.Dqb1, LocusPosition.One, Dqb1.Position1);
            action(Locus.Dqb1, LocusPosition.Two, Dqb1.Position2);
            action(Locus.Drb1, LocusPosition.One, Drb1.Position1);
            action(Locus.Drb1, LocusPosition.Two, Drb1.Position2);
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