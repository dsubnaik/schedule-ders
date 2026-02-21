using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Controllers.Api;

[ApiController]
[Route("api/v1/courses")]
public class CoursesApiController : ControllerBase
{
    private readonly IScheduleQueryService _scheduleQueryService;

    public CoursesApiController(IScheduleQueryService scheduleQueryService)
    {
        _scheduleQueryService = scheduleQueryService;
    }

    [HttpGet("{courseId:int}/sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCourseSessions([FromRoute] int courseId)
    {
        var result = await _scheduleQueryService.GetCourseSessionsAsync(courseId);
        return result is null ? NotFound() : Ok(result);
    }
}
