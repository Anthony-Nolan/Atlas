using NUnit.Framework;

// Fixtures run at the same time; a fixture marked [NonParallelizable] runs alone.
[assembly: Parallelizable(ParallelScope.Fixtures)]

// NUnit runs as many fixtures at once as there are cores by default, which is 2 on a build agent. The tests spend much of their time
// waiting for SQL Server, so more workers than cores keep the cores busy.
[assembly: LevelOfParallelism(4)]
