using ApplicationUpdater.Service.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationUpdater.Service.Controllers;

[ApiController]
[Route("api/updates")]
public sealed class UpdatesController(IUpdateService updateService) : ControllerBase
{
    [HttpGet("/")]
    public ContentResult GetStatus() => Content("UpdateService ist erreichbar.", "text/plain");

    [HttpGet("latest")]
    public ActionResult<UpdateVersion> GetLatest()
    {
        var latestVersion = updateService.GetLatestVersion();
        return latestVersion is null ? NotFound() : Ok(latestVersion);
    }

    [HttpGet("{version}/download")]
    public IActionResult Download([FromRoute] string? version)
    {
        if (!Version.TryParse(version, out var requestedVersion) || requestedVersion.Revision < 0)
        {
            return new JsonResult("Die Version muss aus vier Zahlen bestehen, zum Beispiel 2.0.577.98.")
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var stream = updateService.OpenDownload(requestedVersion);
        return stream is null
            ? NotFound()
            : File(stream, "application/octet-stream", SampleApplication.FileName);
    }
}
