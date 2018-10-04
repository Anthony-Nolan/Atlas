This project can be used to run some repeatable searches against different environments / versions of the algorithm, to provide data useful for improving search performance

The tests are **not** expected to be run by a CI framework

The project is a console C# app, with entry point in `PerformanceHarness.cs`. 
The first argument will specify the test result output directory - if unspecified, they will be stored in the project folder

The test cases, and environment details are held in `TestCases.cs` - to run performance tests against different HLA / environments, update the cases in this file.