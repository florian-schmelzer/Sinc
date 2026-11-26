using ApplicationUpdater.Service.Updates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationUpdater.Service.Controllers;

[ApiController]
[Route("api/updates")]
public sealed class UpdatesController(ISender sender) : ControllerBase
{
    [HttpGet("/")]
    public ContentResult GetStatus() => Content("UpdateService ist erreichbar.", "text/plain");

    [HttpGet("latest")]
    public async Task<ActionResult<UpdateVersion>> GetLatest(CancellationToken cancellationToken)
    {
        var latestVersion = await sender.Send(new GetLatestUpdateQuery(), cancellationToken);
        return latestVersion is null ? NotFound() : Ok(latestVersion);
    }

    [HttpGet("{version}/download")]
    public async Task<IActionResult> Download([FromRoute] string? version, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DownloadUpdateQuery(version), cancellationToken);
        if (result.ValidationError is not null)
        {
            return new JsonResult(result.ValidationError)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        return result.Content is null
            ? NotFound()
            : File(result.Content, "application/octet-stream", SampleApplication.FileName);
    }
}
