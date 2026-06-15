using LMU.Agent.Core.Data;
using LMU.Agent.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllersWithViews();

// EF-Core-Kontext (SQLite, gemeinsamer Pfad mit dem Dienst)
builder.Services.AddScoped<LMUAgentContext>();

// Register Core Services
builder.Services.AddScoped<IEventParser, EventParser>();
builder.Services.AddScoped<IRaceResultParser, RaceResultParser>();
builder.Services.AddScoped<IDriverProfileParser, DriverProfileParser>();
builder.Services.AddScoped<IStatisticsParser, StatisticsParser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// API-Endpunkte (attribut-basiert) und die Stats-Seite (Views).
app.MapControllers();

app.Run();
