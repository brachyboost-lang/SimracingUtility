using LMU.Agent.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

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

// API endpoints are provided by the controllers in Controllers/
// (EventsController, ResultsController, ProfilesController, StatisticsController)
app.MapControllers();

app.Run();
