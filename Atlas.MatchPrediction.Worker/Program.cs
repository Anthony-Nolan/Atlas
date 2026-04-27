using Atlas.MatchPrediction.Worker;

var builder = Host.CreateApplicationBuilder(args);

Startup.Configure(builder.Services);

var host = builder.Build();
host.Run();