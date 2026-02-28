using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Controllers.Api;

[ApiController]
[Route("api/v1/admin/requests")]
[Authorize(Roles = "Admin")]
public class AdminRequestsApiController : ControllerBase
{
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
