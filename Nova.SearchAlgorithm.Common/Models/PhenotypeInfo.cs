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
    public class PhenotypeInfo<T> : Utils.PhenoTypeInfo.PhenotypeInfo<T>
    {
        public PhenotypeInfo()
        {
        }

        public PhenotypeInfo(T initialValue) : base(initialValue)
        {
        }

        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
        public PhenotypeInfo<R> Map<R>(Func<Locus, TypePositions, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A_1 = mapping(Locus.A, TypePositions.One, A_1),
                A_2 = mapping(Locus.A, TypePositions.Two, A_2),
                B_1 = mapping(Locus.B, TypePositions.One, B_1),
                B_2 = mapping(Locus.B, TypePositions.Two, B_2),
                C_1 = mapping(Locus.C, TypePositions.One, C_1),
                C_2 = mapping(Locus.C, TypePositions.Two, C_2),
                Dpb1_1 = mapping(Locus.Dpb1, TypePositions.One, Dpb1_1),
                Dpb1_2 = mapping(Locus.Dpb1, TypePositions.Two, Dpb1_2),
                Dqb1_1 = mapping(Locus.Dqb1, TypePositions.One, Dqb1_1),
                Dqb1_2 = mapping(Locus.Dqb1, TypePositions.Two, Dqb1_2),
                Drb1_1 = mapping(Locus.Drb1, TypePositions.One, Drb1_1),
                Drb1_2 = mapping(Locus.Drb1, TypePositions.Two, Drb1_2),
            };
        }

        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
        public PhenotypeInfo<R> MapByLocus<R>(Func<Locus, T, T, Tuple<R, R>> mapping)
        {
            var a = mapping(Locus.A, A_1, A_2);
            var b = mapping(Locus.B, B_1, B_2);
            var c = mapping(Locus.C, C_1, C_2);
            var dpb1 = mapping(Locus.Dpb1, Dpb1_1, Dpb1_2);
            var dqb1 = mapping(Locus.Dqb1, Dqb1_1, Dqb1_2);
            var drb1 = mapping(Locus.Drb1, Drb1_1, Drb1_2);

            return new PhenotypeInfo<R>
            {
                A_1 = a.Item1,
                A_2 = a.Item2,
                B_1 = b.Item1,
                B_2 = b.Item2,
                C_1 = c.Item1,
                C_2 = c.Item2,
                Dpb1_1 = dpb1.Item1,
                Dpb1_2 = dpb1.Item2,
                Dqb1_1 = dqb1.Item1,
                Dqb1_2 = dqb1.Item2,
                Drb1_1 = drb1.Item1,
                Drb1_2 = drb1.Item2,
            };
        }

        public async Task<PhenotypeInfo<R>> MapAsync<R>(Func<Locus, TypePositions, T, Task<R>> mapping)
        {
            var data = new[]
            {
                new PositionInfo<T>(Locus.A, TypePositions.One, A_1),
                new PositionInfo<T>(Locus.A, TypePositions.Two, A_2),
                new PositionInfo<T>(Locus.B, TypePositions.One, B_1),
                new PositionInfo<T>(Locus.B, TypePositions.Two, B_2),
                new PositionInfo<T>(Locus.C, TypePositions.One, C_1),
                new PositionInfo<T>(Locus.C, TypePositions.Two, C_2),
                new PositionInfo<T>(Locus.Dpb1, TypePositions.One, Dpb1_1),
                new PositionInfo<T>(Locus.Dpb1, TypePositions.Two, Dpb1_2),
                new PositionInfo<T>(Locus.Dqb1, TypePositions.One, Dqb1_1),
                new PositionInfo<T>(Locus.Dqb1, TypePositions.Two, Dqb1_2),
                new PositionInfo<T>(Locus.Drb1, TypePositions.One, Drb1_1),
                new PositionInfo<T>(Locus.Drb1, TypePositions.Two, Drb1_2)
            };

            var results = await Task.WhenAll(data.Select(async d =>
            {
                var result = await mapping(d.Locus, d.Position, d.Data);
                return new PositionInfo<R>(d.Locus, d.Position, result);
            }));

            return new PhenotypeInfo<R>
            {
                A_1 = results.Single(r => r.Locus == Locus.A && r.Position == TypePositions.One).Data,
                A_2 = results.Single(r => r.Locus == Locus.A && r.Position == TypePositions.Two).Data,
                B_1 = results.Single(r => r.Locus == Locus.B && r.Position == TypePositions.One).Data,
                B_2 = results.Single(r => r.Locus == Locus.B && r.Position == TypePositions.Two).Data,
                C_1 = results.Single(r => r.Locus == Locus.C && r.Position == TypePositions.One).Data,
                C_2 = results.Single(r => r.Locus == Locus.C && r.Position == TypePositions.Two).Data,
                Dpb1_1 = results.Single(r => r.Locus == Locus.Dpb1 && r.Position == TypePositions.One).Data,
                Dpb1_2 = results.Single(r => r.Locus == Locus.Dpb1 && r.Position == TypePositions.Two).Data,
                Dqb1_1 = results.Single(r => r.Locus == Locus.Dqb1 && r.Position == TypePositions.One).Data,
                Dqb1_2 = results.Single(r => r.Locus == Locus.Dqb1 && r.Position == TypePositions.Two).Data,
                Drb1_1 = results.Single(r => r.Locus == Locus.Drb1 && r.Position == TypePositions.One).Data,
                Drb1_2 = results.Single(r => r.Locus == Locus.Drb1 && r.Position == TypePositions.Two).Data,
            };
        }

        // Aggregates each locus alongside its two values
        public IEnumerable<R> FlatMap<R>(Func<Locus, T, T, R> mapping)
        {
            return new List<R>
            {
                mapping(Locus.A, A_1, A_2),
                mapping(Locus.B, B_1, B_2),
                mapping(Locus.C, C_1, C_2),
                mapping(Locus.Dpb1, Dpb1_1, Dpb1_2),
                mapping(Locus.Dqb1, Dqb1_1, Dqb1_2),
                mapping(Locus.Drb1, Drb1_1, Drb1_2),
            };
        }

        public void EachPosition(Action<Locus, TypePositions, T> action)
        {
            action(Locus.A, TypePositions.One, A_1);
            action(Locus.A, TypePositions.Two, A_2);
            action(Locus.B, TypePositions.One, B_1);
            action(Locus.B, TypePositions.Two, B_2);
            action(Locus.C, TypePositions.One, C_1);
            action(Locus.C, TypePositions.Two, C_2);
            action(Locus.Dpb1, TypePositions.One, Dpb1_1);
            action(Locus.Dpb1, TypePositions.Two, Dpb1_2);
            action(Locus.Dqb1, TypePositions.One, Dqb1_1);
            action(Locus.Dqb1, TypePositions.Two, Dqb1_2);
            action(Locus.Drb1, TypePositions.One, Drb1_1);
            action(Locus.Drb1, TypePositions.Two, Drb1_2);
        }

        public void EachLocus(Action<Locus, T, T> action)
        {
            action(Locus.A, A_1, A_2);
            action(Locus.B, B_1, B_2);
            action(Locus.C, C_1, C_2);
            action(Locus.Dpb1, Dpb1_1, Dpb1_2);
            action(Locus.Dqb1, Dqb1_1, Dqb1_2);
            action(Locus.Drb1, Drb1_1, Drb1_2);
        }

        public async Task WhenAllLoci(Func<Locus, T, T, Task> action)
        {
            await Task.WhenAll(
                action(Locus.A, A_1, A_2),
                action(Locus.B, B_1, B_2),
                action(Locus.C, C_1, C_2),
                action(Locus.Dpb1, Dpb1_1, Dpb1_2),
                action(Locus.Dqb1, Dqb1_1, Dqb1_2),
                action(Locus.Drb1, Drb1_1, Drb1_2));
        }

        public async Task<PhenotypeInfo<R>> WhenAllLoci<R>(Func<Locus, T, T, Task<Tuple<R, R>>> action)
        {
            var results = await Task.WhenAll(
                action(Locus.A, A_1, A_2),
                action(Locus.B, B_1, B_2),
                action(Locus.C, C_1, C_2),
                action(Locus.Dpb1, Dpb1_1, Dpb1_2),
                action(Locus.Dqb1, Dqb1_1, Dqb1_2),
                action(Locus.Drb1, Drb1_1, Drb1_2));

            return new PhenotypeInfo<R>
            {
                A_1 = results[0].Item1,
                A_2 = results[0].Item2,
                B_1 = results[1].Item1,
                B_2 = results[1].Item2,
                C_1 = results[2].Item1,
                C_2 = results[2].Item2,
                Dpb1_1 = results[3].Item1,
                Dpb1_2 = results[3].Item2,
                Dqb1_1 = results[4].Item1,
                Dqb1_2 = results[4].Item2,
                Drb1_1 = results[5].Item1,
                Drb1_2 = results[5].Item2
            };
        }

        public Tuple<T, T> DataAtLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return new Tuple<T, T>(A_1, A_2);
                case Locus.B:
                    return new Tuple<T, T>(B_1, B_2);
                case Locus.C:
                    return new Tuple<T, T>(C_1, C_2);
                case Locus.Dpb1:
                    return new Tuple<T, T>(Dpb1_1, Dpb1_2);
                case Locus.Dqb1:
                    return new Tuple<T, T>(Dqb1_1, Dqb1_2);
                case Locus.Drb1:
                    return new Tuple<T, T>(Drb1_1, Drb1_2);
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public T DataAtPosition(Locus locus, TypePositions position)
        {
            const string errorMessage = "Can only fetch a single piece of data at a specific position";
            switch (locus)
            {
                case Locus.A:
                    switch (position)
                    {
                        case TypePositions.One:
                            return A_1;
                        case TypePositions.Two:
                            return A_2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.B:
                    switch (position)
                    {
                        case TypePositions.One:
                            return B_1;
                        case TypePositions.Two:
                            return B_2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.C:
                    switch (position)
                    {
                        case TypePositions.One:
                            return C_1;
                        case TypePositions.Two:
                            return C_2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.Dpb1:
                    switch (position)
                    {
                        case TypePositions.One:
                            return Dpb1_1;
                        case TypePositions.Two:
                            return Dpb1_2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.Dqb1:
                    switch (position)
                    {
                        case TypePositions.One:
                            return Dqb1_1;
                        case TypePositions.Two:
                            return Dqb1_2;
                        default:
                            throw new Exception(errorMessage);
                    }
                case Locus.Drb1:
                    switch (position)
                    {
                        case TypePositions.One:
                            return Drb1_1;
                        case TypePositions.Two:
                            return Drb1_2;
                        default:
                            throw new Exception(errorMessage);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public void SetAtPosition(Locus locus, TypePositions positions, T value)
        {
            switch (locus)
            {
                case Locus.A:
                    switch (positions)
                    {
                        case TypePositions.One:
                            A_1 = value;
                            break;
                        case TypePositions.Two:
                            A_2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(positions), positions, null);
                    }

                    break;
                case Locus.B:
                    switch (positions)
                    {
                        case TypePositions.One:
                            B_1 = value;
                            break;
                        case TypePositions.Two:
                            B_2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(positions), positions, null);
                    }

                    break;
                case Locus.C:
                    switch (positions)
                    {
                        case TypePositions.One:
                            C_1 = value;
                            break;
                        case TypePositions.Two:
                            C_2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(positions), positions, null);
                    }

                    break;
                case Locus.Dpb1:
                    switch (positions)
                    {
                        case TypePositions.One:
                            Dpb1_1 = value;
                            break;
                        case TypePositions.Two:
                            Dpb1_2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(positions), positions, null);
                    }

                    break;
                case Locus.Dqb1:
                    switch (positions)
                    {
                        case TypePositions.One:
                            Dqb1_1 = value;
                            break;
                        case TypePositions.Two:
                            Dqb1_2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(positions), positions, null);
                    }

                    break;
                case Locus.Drb1:
                    switch (positions)
                    {
                        case TypePositions.One:
                            Drb1_1 = value;
                            break;
                        case TypePositions.Two:
                            Drb1_2 = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(positions), positions, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public void SetAtLocus(Locus locus, T value)
        {
            SetAtPosition(locus, TypePositions.One, value);
            SetAtPosition(locus, TypePositions.Two, value);
        }

        private class PositionInfo<R>
        {
            public Locus Locus { get; }
            public TypePositions Position { get; }
            public R Data { get; }

            public PositionInfo(Locus locus, TypePositions position, R data)
            {
                Locus = locus;
                Position = position;
                Data = data;
            }
        }
    }
}