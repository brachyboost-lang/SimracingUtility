using Microsoft.AspNetCore.Mvc;
using LMU.Agent.Core.Services;
using LMU.Agent.Core.Models;

namespace LMU.Agent.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly IDriverProfileParser _driverProfileParser;

    public ProfilesController(IDriverProfileParser driverProfileParser)
    {
        _driverProfileParser = driverProfileParser;
    }

    [HttpGet]
    public async Task<ActionResult<List<DriverProfile>>> GetDriverProfiles()
    {
        var profiles = await _driverProfileParser.GetDriverProfilesAsync();
        return Ok(profiles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DriverProfile>> GetDriverProfile(int id)
    {
        var profile = await _driverProfileParser.GetDriverProfileByIdAsync(id);
        if (profile == null)
        {
            return NotFound();
        }
        return Ok(profile);
    }
}