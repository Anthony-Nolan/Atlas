using NUnit.Framework;

// Fixtures run at the same time; a fixture marked [NonParallelizable] runs alone.
[assembly: Parallelizable(ParallelScope.Fixtures)]
