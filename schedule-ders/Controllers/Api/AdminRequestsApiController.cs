using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Controllers.Api;

[ApiController]
[Route("api/v1/admin/requests")]
[Authorize(Roles = "Admin")]
[RequestSizeLimit(65_536)]
public class AdminRequestsApiController : ControllerBase
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending", "underreview", "under review", "approved", "denied"
    };

    private readonly IAdminRequestService _adminRequestService;

    public AdminRequestsApiController(IAdminRequestService adminRequestService)
    {
        _adminRequestService = adminRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? status,
        [FromQuery] string? course,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (status is not null && !ValidStatuses.Contains(status))
            return BadRequest(new { message = "Invalid status value." });

        if (course is not null && course.Length > 120)
            return BadRequest(new { message = "course filter must be 120 characters or fewer." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _adminRequestService.GetRequestsAsync(status, course, from, to, page, pageSize);
        return Ok(result);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateRequestStatusDto input)
    {
        var updated = await _adminRequestService.UpdateStatusAsync(id, input);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveRequest([FromRoute] int id)
    {
        var result = await _adminRequestService.RemoveRequestAsync(id);
        return result switch
        {
            RemoveAdminRequestResult.NotFound => NotFound(),
            RemoveAdminRequestResult.NotAllowed => BadRequest(new { message = "Only approved or denied requests can be removed." }),
            _ => NoContent()
        };
    }
}
