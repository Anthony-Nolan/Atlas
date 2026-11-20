using NUnit.Framework;

// Features run at the same time; each NUnit worker has its own copy of the test database (see WorkerDatabases), in the tests and in the API.
// NUnit runs as many at once as there are cores, unless NUnit.NumberOfTestWorkers says otherwise, as the build pipeline does.
[assembly: Parallelizable(ParallelScope.Fixtures)]
