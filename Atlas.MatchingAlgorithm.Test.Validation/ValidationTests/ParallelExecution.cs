using NUnit.Framework;

// Each feature is a test fixture, and features run in parallel; the scenarios within a feature run one at a time.
// Each scenario has its own dependency injection scope (see Hooks), and the test data is set up once, before any scenario runs.
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]
