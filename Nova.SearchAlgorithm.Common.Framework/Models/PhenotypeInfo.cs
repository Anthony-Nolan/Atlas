using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Common.Models
{
    /// <summary>
    /// Data type to hold one instance of T for each of the supported HLA loci and each type position within.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each loci position.</typeparam>
    public class PhenotypeInfo<T> : Utils.PhenotypeInfo.PhenotypeInfo<T>
    {
        public PhenotypeInfo()
        {
        }

        public PhenotypeInfo(T initialValue) : base(initialValue)
        {
        }

        public PhenotypeInfo(Utils.PhenotypeInfo.PhenotypeInfo<T> phenotypeInfo)
        {
            A.Position1 = phenotypeInfo.A.Position1;
            A.Position2 = phenotypeInfo.A.Position2;
            B.Position1 = phenotypeInfo.B.Position1;
            B.Position2 = phenotypeInfo.B.Position2;
            C.Position1 = phenotypeInfo.C.Position1;
            C.Position2 = phenotypeInfo.C.Position2;
            Dpb1.Position1 = phenotypeInfo.Dpb1.Position1;
            Dpb1.Position2 = phenotypeInfo.Dpb1.Position2;
            Dqb1.Position1 = phenotypeInfo.Dqb1.Position1;
            Dqb1.Position2 = phenotypeInfo.Dqb1.Position2;
            Drb1.Position1 = phenotypeInfo.Drb1.Position1;
            Drb1.Position2 = phenotypeInfo.Drb1.Position2;
        }

        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
        public PhenotypeInfo<R> Map<R>(Func<Locus, TypePosition, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A =
                {
                    Position1 = mapping(Locus.A, TypePosition.One, A.Position1),
                    Position2 = mapping(Locus.A, TypePosition.Two, A.Position2),
                },
                B =
                {
                    Position1 = mapping(Locus.B, TypePosition.One, B.Position1),
                    Position2 = mapping(Locus.B, TypePosition.Two, B.Position2),
                },
                C =
                {
                    Position1 = mapping(Locus.C, TypePosition.One, C.Position1),
                    Position2 = mapping(Locus.C, TypePosition.Two, C.Position2),
                },
                Dpb1 =
                {
                    Position1 = mapping(Locus.Dpb1, TypePosition.One, Dpb1.Position1),
                    Position2 = mapping(Locus.Dpb1, TypePosition.Two, Dpb1.Position2),
                },
                Dqb1 =
                {
                    Position1 = mapping(Locus.Dqb1, TypePosition.One, Dqb1.Position1),
                    Position2 = mapping(Locus.Dqb1, TypePosition.Two, Dqb1.Position2),
                },
                Drb1 =
                {
                    Position1 = mapping(Locus.Drb1, TypePosition.One, Drb1.Position1),
                    Position2 = mapping(Locus.Drb1, TypePosition.Two, Drb1.Position2),
                }
            };
        }

        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
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

        public async Task<PhenotypeInfo<R>> MapAsync<R>(Func<Locus, TypePosition, T, Task<R>> mapping)
        {
            var data = new[]
            {
                new PositionInfo<T>(Locus.A, TypePosition.One, A.Position1),
                new PositionInfo<T>(Locus.A, TypePosition.Two, A.Position2),
                new PositionInfo<T>(Locus.B, TypePosition.One, B.Position1),
                new PositionInfo<T>(Locus.B, TypePosition.Two, B.Position2),
                new PositionInfo<T>(Locus.C, TypePosition.One, C.Position1),
                new PositionInfo<T>(Locus.C, TypePosition.Two, C.Position2),
                new PositionInfo<T>(Locus.Dpb1, TypePosition.One, Dpb1.Position1),
                new PositionInfo<T>(Locus.Dpb1, TypePosition.Two, Dpb1.Position2),
                new PositionInfo<T>(Locus.Dqb1, TypePosition.One, Dqb1.Position1),
                new PositionInfo<T>(Locus.Dqb1, TypePosition.Two, Dqb1.Position2),
                new PositionInfo<T>(Locus.Drb1, TypePosition.One, Drb1.Position1),
                new PositionInfo<T>(Locus.Drb1, TypePosition.Two, Drb1.Position2)
            };

            var results = await Task.WhenAll(data.Select(async d =>
            {
                var result = await mapping(d.Locus, d.Position, d.Data);
                return new PositionInfo<R>(d.Locus, d.Position, result);
            }));

            return new PhenotypeInfo<R>
            {
                A =
                {
                    Position1 = results.Single(r => r.Locus == Locus.A && r.Position == TypePosition.One).Data,
                    Position2 = results.Single(r => r.Locus == Locus.A && r.Position == TypePosition.Two).Data,
                },
                B =
                {
                    Position1 = results.Single(r => r.Locus == Locus.B && r.Position == TypePosition.One).Data,
                    Position2 = results.Single(r => r.Locus == Locus.B && r.Position == TypePosition.Two).Data,
                },
                C =
                {
                    Position1 = results.Single(r => r.Locus == Locus.C && r.Position == TypePosition.One).Data,
                    Position2 = results.Single(r => r.Locus == Locus.C && r.Position == TypePosition.Two).Data,
                },
                Dpb1 =
                {
                    Position1 = results.Single(r => r.Locus == Locus.Dpb1 && r.Position == TypePosition.One).Data,
                    Position2 = results.Single(r => r.Locus == Locus.Dpb1 && r.Position == TypePosition.Two).Data,
                },
                Dqb1 =
                {
                    Position1 = results.Single(r => r.Locus == Locus.Dqb1 && r.Position == TypePosition.One).Data,
                    Position2 = results.Single(r => r.Locus == Locus.Dqb1 && r.Position == TypePosition.Two).Data,
                },
                Drb1 =
                {
                    Position1 = results.Single(r => r.Locus == Locus.Drb1 && r.Position == TypePosition.One).Data,
                    Position2 = results.Single(r => r.Locus == Locus.Drb1 && r.Position == TypePosition.Two).Data,
                }
            };
        }

        // Aggregates each locus alongside its two values
        public IEnumerable<R> FlatMap<R>(Func<Locus, T, T, R> mapping)
        {
            return new List<R>
            {
                mapping(Locus.A, A.Position1, A.Position2),
                mapping(Locus.B, B.Position1, B.Position2),
                mapping(Locus.C, C.Position1, C.Position2),
                mapping(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2),
                mapping(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2),
                mapping(Locus.Drb1, Drb1.Position1, Drb1.Position2),
            };
        }

        public void EachPosition(Action<Locus, TypePosition, T> action)
        {
            action(Locus.A, TypePosition.One, A.Position1);
            action(Locus.A, TypePosition.Two, A.Position2);
            action(Locus.B, TypePosition.One, B.Position1);
            action(Locus.B, TypePosition.Two, B.Position2);
            action(Locus.C, TypePosition.One, C.Position1);
            action(Locus.C, TypePosition.Two, C.Position2);
            action(Locus.Dpb1, TypePosition.One, Dpb1.Position1);
            action(Locus.Dpb1, TypePosition.Two, Dpb1.Position2);
            action(Locus.Dqb1, TypePosition.One, Dqb1.Position1);
            action(Locus.Dqb1, TypePosition.Two, Dqb1.Position2);
            action(Locus.Drb1, TypePosition.One, Drb1.Position1);
            action(Locus.Drb1, TypePosition.Two, Drb1.Position2);
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

        public async Task<PhenotypeInfo<R>> WhenAllLoci<R>(Func<Locus, T, T, Task<Tuple<R, R>>> action)
        {
            var results = await Task.WhenAll(
                action(Locus.A, A.Position1, A.Position2),
                action(Locus.B, B.Position1, B.Position2),
                action(Locus.C, C.Position1, C.Position2),
                action(Locus.Dpb1, Dpb1.Position1, Dpb1.Position2),
                action(Locus.Dqb1, Dqb1.Position1, Dqb1.Position2),
                action(Locus.Drb1, Drb1.Position1, Drb1.Position2));

            return new PhenotypeInfo<R>
            {
                A =
                {
                    Position1 = results[0].Item1,
                    Position2 = results[0].Item2,
                },
                B =
                {
                    Position1 = results[1].Item1,
                    Position2 = results[1].Item2,
                },
                C =
                {
                    Position1 = results[2].Item1,
                    Position2 = results[2].Item2,
                },
                Dpb1 =
                {
                    Position1 = results[3].Item1,
                    Position2 = results[3].Item2,
                },
                Dqb1 =
                {
                    Position1 = results[4].Item1,
                    Position2 = results[4].Item2,
                },
                Drb1 =
                {
                    Position1 = results[5].Item1,
                    Position2 = results[5].Item2
                }
            };
        }

        public Tuple<T, T> DataAtLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return new Tuple<T, T>(A.Position1, A.Position2);
                case Locus.B:
                    return new Tuple<T, T>(B.Position1, B.Position2);
                case Locus.C:
                    return new Tuple<T, T>(C.Position1, C.Position2);
                case Locus.Dpb1:
                    return new Tuple<T, T>(Dpb1.Position1, Dpb1.Position2);
                case Locus.Dqb1:
                    return new Tuple<T, T>(Dqb1.Position1, Dqb1.Position2);
                case Locus.Drb1:
                    return new Tuple<T, T>(Drb1.Position1, Drb1.Position2);
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public T DataAtPosition(Locus locus, TypePosition position)
        {
            const string errorMessage = "Can only fetch a single piece of data at a specific position";
            switch (locus)
            {
                case Locus.A:
                    switch (position)
                    {
                        case TypePosition.One:
                            return A.Position1;
                        case TypePosition.Two:
                            return A.Position2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.B:
                    switch (position)
                    {
                        case TypePosition.One:
                            return B.Position1;
                        case TypePosition.Two:
                            return B.Position2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.C:
                    switch (position)
                    {
                        case TypePosition.One:
                            return C.Position1;
                        case TypePosition.Two:
                            return C.Position2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.Dpb1:
                    switch (position)
                    {
                        case TypePosition.One:
                            return Dpb1.Position1;
                        case TypePosition.Two:
                            return Dpb1.Position2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.Dqb1:
                    switch (position)
                    {
                        case TypePosition.One:
                            return Dqb1.Position1;
                        case TypePosition.Two:
                            return Dqb1.Position2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.Drb1:
                    switch (position)
                    {
                        case TypePosition.One:
                            return Drb1.Position1;
                        case TypePosition.Two:
                            return Drb1.Position2;
                        default:
                            throw new Exception(errorMessage);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public void SetAtPosition(Locus locus, TypePosition position, T value)
        {
            switch (locus)
            {
                case Locus.A:
                    switch (position)
                    {
                        case TypePosition.One:
                            A.Position1 = value;
                            break;
                        case TypePosition.Two:
                            A.Position2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.B:
                    switch (position)
                    {
                        case TypePosition.One:
                            B.Position1 = value;
                            break;
                        case TypePosition.Two:
                            B.Position2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.C:
                    switch (position)
                    {
                        case TypePosition.One:
                            C.Position1 = value;
                            break;
                        case TypePosition.Two:
                            C.Position2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.Dpb1:
                    switch (position)
                    {
                        case TypePosition.One:
                            Dpb1.Position1 = value;
                            break;
                        case TypePosition.Two:
                            Dpb1.Position2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.Dqb1:
                    switch (position)
                    {
                        case TypePosition.One:
                            Dqb1.Position1 = value;
                            break;
                        case TypePosition.Two:
                            Dqb1.Position2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.Drb1:
                    switch (position)
                    {
                        case TypePosition.One:
                            Drb1.Position1 = value;
                            break;
                        case TypePosition.Two:
                            Drb1.Position2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public void SetAtLocus(Locus locus, T value)
        {
            SetAtPosition(locus, TypePosition.One, value);
            SetAtPosition(locus, TypePosition.Two, value);
        }

        private class PositionInfo<R>
        {
            public Locus Locus { get; }
            public TypePosition Position { get; }
            public R Data { get; }

            public PositionInfo(Locus locus, TypePosition position, R data)
            {
                Locus = locus;
                Position = position;
                Data = data;
            }
        }
    }
}