using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Services.Interfaces;

namespace schedule_ders.Controllers.Api;

[ApiController]
[Route("api/v1/professor/requests")]
[Authorize(Roles = "Professor")]
public class ProfessorRequestsApiController : ControllerBase
{
    private readonly IProfessorRequestService _professorRequestService;

    public ProfessorRequestsApiController(IProfessorRequestService professorRequestService)
    {
        _professorRequestService = professorRequestService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSiRequestDto input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var (professorName, professorEmail) = ResolveProfessorIdentity();
            input.ProfessorName = professorName;
            input.ProfessorEmail = professorEmail;
            input.RequestedCourseProfessor = professorName;

            var result = await _professorRequestService.CreateRequestAsync(input, userId);
            return CreatedAtAction(nameof(GetStatus), new { id = result.RequestId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/status")]
    public async Task<IActionResult> GetStatus([FromRoute] int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _professorRequestService.GetOwnedStatusAsync(id, userId);

        if (!result.Exists)
        {
            return NotFound();
        }

        if (!result.IsOwner)
        {
            return Forbid();
        }

        return Ok(result.Status);
    }

    private (string Name, string Email) ResolveProfessorIdentity()
    {
        var email = (User.Identity?.Name ?? string.Empty).Trim();
        var localPart = email.Contains('@') ? email.Split('@')[0] : email;
        var safeLocalPart = string.IsNullOrWhiteSpace(localPart) ? "Professor" : localPart;
        var displayName = safeLocalPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Professor";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            email = "professor@email.com";
        }

        return (displayName, email);
    }
}
