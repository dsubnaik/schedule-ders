using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Controllers.Api;

[ApiController]
[Route("api/v1/schedule")]
public class ScheduleApiController : ControllerBase
{
    private readonly IScheduleQueryService _scheduleQueryService;

    public ScheduleApiController(IScheduleQueryService scheduleQueryService)
    {
        _scheduleQueryService = scheduleQueryService;
    }

    [HttpGet("sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSessions(
        [FromQuery] string? search,
        [FromQuery] string? day,
        [FromQuery] string? professor,
        [FromQuery] int? courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _scheduleQueryService.SearchSessionsAsync(search, day, professor, courseId, page, pageSize);
        return Ok(result);
    }
}
