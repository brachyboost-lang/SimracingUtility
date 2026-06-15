using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Services;

public interface IRaceResultParser
{
    Task<List<RaceResult>> ParseRaceResultsAsync(string savePath);
    Task<List<RaceResult>> GetLastRaceResultsAsync();
    Task<RaceResult?> GetRaceResultByIdAsync(int id);
}
