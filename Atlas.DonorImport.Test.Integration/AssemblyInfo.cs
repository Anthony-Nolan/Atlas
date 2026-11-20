using NUnit.Framework;

// Fixtures run at the same time; each NUnit worker has its own copy of the database (see WorkerDatabases).
// NUnit runs as many at once as there are cores, unless NUnit.NumberOfTestWorkers says otherwise, as the build pipeline does.
[assembly: Parallelizable(ParallelScope.Fixtures)]
