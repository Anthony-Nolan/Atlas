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
        private const string LocusInfoNullExceptionMessage =
            "LocusInfo<T> cannot be null in a PhenotypeInfo<T>. Set the nested values to null instead.";

        #region Overrides of LociInfo<LocusInfo<T>>

        /// <inheritdoc />
        public override LocusInfo<T> A
        {
            get => a;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), LocusInfoNullExceptionMessage);
                }

                a = value;
            }
        }
        /// <inheritdoc />
        public override LocusInfo<T> B
        {
            get => b;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), LocusInfoNullExceptionMessage);
                }

                b = value;
            }
        }
        /// <inheritdoc />
        public override LocusInfo<T> C
        {
            get => c;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), LocusInfoNullExceptionMessage);
                }

                c = value;
            }
        }
        /// <inheritdoc />
        public override LocusInfo<T> Dpb1
        {
            get => dpb1;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), LocusInfoNullExceptionMessage);
                }

                dpb1 = value;
            }
        }
        /// <inheritdoc />
        public override LocusInfo<T> Dqb1
        {
            get => dqb1;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), LocusInfoNullExceptionMessage);
                }

                dqb1 = value;
            }
        }
        /// <inheritdoc />
        public override LocusInfo<T> Drb1
        {
            get => drb1;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), LocusInfoNullExceptionMessage);
                }

                drb1 = value;
            }
        }

        #endregion

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
            a = source.A.ShallowCopy();
            b = source.B.ShallowCopy();
            c = source.C.ShallowCopy();
            dpb1 = source.Dpb1.ShallowCopy();
            dqb1 = source.Dqb1.ShallowCopy();
            drb1 = source.Drb1.ShallowCopy();
        }

        /// <summary>
        /// Creates a new PhenotypeInfo with all inner values set to the same starting value.
        /// </summary>
        /// <param name="initialValue">The initial value all inner locus position values should be given.</param>
        public PhenotypeInfo(T initialValue)
        {
            Initialise();
            SetLocus(Locus.A, initialValue);
            SetLocus(Locus.B, initialValue);
            SetLocus(Locus.C, initialValue);
            SetLocus(Locus.Dpb1, initialValue);
            SetLocus(Locus.Dqb1, initialValue);
            SetLocus(Locus.Drb1, initialValue);
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

        public PhenotypeInfo<R> MapByLocus<R>(Func<LocusInfo<T>, LocusInfo<R>> mapping)
        {
            return MapByLocus((locus, x) => mapping(x));
        }

        public PhenotypeInfo<R> MapByLocus<R>(Func<Locus, LocusInfo<T>, LocusInfo<R>> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A = mapping(Locus.A, A),
                B = mapping(Locus.B, B),
                C = mapping(Locus.C, C),
                Dpb1 = mapping(Locus.Dpb1, Dpb1),
                Dqb1 = mapping(Locus.Dqb1, Dqb1),
                Drb1 = mapping(Locus.Drb1, Drb1)
            };
        }

        public async Task<PhenotypeInfo<R>> MapByLocusAsync<R>(Func<Locus, LocusInfo<T>, Task<LocusInfo<R>>> action)
        {
            var a = action(Locus.A, A);
            var b = action(Locus.B, B);
            var c = action(Locus.C, C);
            var dpb1 = action(Locus.Dpb1, Dpb1);
            var dqb1 = action(Locus.Dqb1, Dqb1);
            var drb1 = action(Locus.Drb1, Drb1);

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

        public LociInfo<R> ToLociInfo<R>(Func<Locus, T, T, R> combine)
        {
            return Map((l, hla) => combine(l, hla.Position1, hla.Position2));
        }

        public R Reduce<R>(Func<Locus, LocusPosition, T, R, R> reducer, R initialValue = default)
        {
            return Reduce((locus, t, accumulator) =>
            {
                accumulator = reducer(locus, LocusPosition.One, t.Position1, accumulator);
                accumulator = reducer(locus, LocusPosition.Two, t.Position2, accumulator);
                return accumulator;
            }, initialValue);
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

        public void EachLocus(Action<Locus, LocusInfo<T>> action)
        {
            action(Locus.A, A);
            action(Locus.B, B);
            action(Locus.C, C);
            action(Locus.Dpb1, Dpb1);
            action(Locus.Dqb1, Dqb1);
            action(Locus.Drb1, Drb1);
        }

        public async Task EachLocusAsync(Func<Locus, LocusInfo<T>, Task> action)
        {
            await Task.WhenAll(
                action(Locus.A, A),
                action(Locus.B, B),
                action(Locus.C, C),
                action(Locus.Dpb1, Dpb1),
                action(Locus.Dqb1, Dqb1),
                action(Locus.Drb1, Drb1));
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

        public async Task WhenAllLoci(Func<Locus, LocusInfo<T>, Task> action)
        {
            await Task.WhenAll(
                action(Locus.A, A),
                action(Locus.B, B),
                action(Locus.C, C),
                action(Locus.Dpb1, Dpb1),
                action(Locus.Dqb1, Dqb1),
                action(Locus.Drb1, Drb1));
        }

        private void Initialise()
        {
            A = new LocusInfo<T>();
            B = new LocusInfo<T>();
            C = new LocusInfo<T>();
            Dpb1 = new LocusInfo<T>();
            Dqb1 = new LocusInfo<T>();
            Drb1 = new LocusInfo<T>();
        }
    }
}