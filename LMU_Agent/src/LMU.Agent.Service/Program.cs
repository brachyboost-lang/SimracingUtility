using LMU.Agent.Core.Data;
using LMU.Agent.Core.Services;
using LMU.Agent.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Als Windows-Dienst lauffähig (und weiterhin als Konsole startbar zum Debuggen)
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LMU Agent";
});

// EF-Core-Kontext (SQLite wird via OnConfiguring in LMUAgentContext gesetzt)
builder.Services.AddScoped<LMUAgentContext>();

// Core-Parser registrieren
builder.Services.AddScoped<IEventParser, EventParser>();
builder.Services.AddScoped<IRaceResultParser, RaceResultParser>();
builder.Services.AddScoped<IDriverProfileParser, DriverProfileParser>();
builder.Services.AddScoped<IStatisticsParser, StatisticsParser>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
