using LMU.Agent.Core.Models;

namespace LMU.Agent.Core.Services;

public interface IDriverProfileParser
{
    Task<List<DriverProfile>> ParseProfilesAsync(string profilePath);
    Task<List<DriverProfile>> GetDriverProfilesAsync();
    Task<DriverProfile?> GetDriverProfileByIdAsync(int id);
}
