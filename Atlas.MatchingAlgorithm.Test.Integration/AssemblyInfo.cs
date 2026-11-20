using NUnit.Framework;

// Fixtures run at the same time; one that needs databases to itself derives from FixtureWithPooledDatabases.
// NUnit runs as many at once as there are cores, unless NUnit.NumberOfTestWorkers says otherwise, as the build pipeline does.
[assembly: Parallelizable(ParallelScope.Fixtures)]
