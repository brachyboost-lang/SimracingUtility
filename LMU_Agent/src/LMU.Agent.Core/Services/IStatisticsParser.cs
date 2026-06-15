using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Services;

public interface IStatisticsParser
{
    Task<List<Statistics>> CalculateStatisticsAsync();
    Task<Statistics?> GetStatisticsByDriverNameAsync(string driverName);
    Task<Statistics?> CalculateAndStoreStatisticsAsync();
}