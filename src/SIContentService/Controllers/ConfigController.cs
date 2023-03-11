using Microsoft.AspNetCore.Mvc;

namespace SIContentService.Controllers;

/// <summary>
/// Provides information about application configuration.
/// </summary>
[Route("config")]
[ApiController]
public sealed class ConfigController : ControllerBase
{
	private readonly IConfiguration _configuration;

	public ConfigController(IConfiguration configuration) => _configuration = configuration;

    /// <summary>
    /// Gets configuration values.
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok(_configuration.AsEnumerable().ToDictionary(p => p.Key, p => p.Value));
}
