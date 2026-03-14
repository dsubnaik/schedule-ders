using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class SILeadersController : Controller
{
    private readonly ScheduleContext _context;

    public SILeadersController(ScheduleContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var vm = await BuildIndexViewModelAsync(search);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SILeaderAdminIndexViewModel input)
    {
        var aNumber = (input.CreateANumber ?? string.Empty).Trim();
        var leaderName = (input.CreateLeaderName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(aNumber) || string.IsNullOrWhiteSpace(leaderName))
        {
            TempData["SILeaderError"] = "A-Number and SI Leader Name are required.";
            return RedirectToAction(nameof(Index), new { search = input.Search });
        }

        var exists = await _context.SILeaders
            .AnyAsync(l => l.ANumber == aNumber);

        if (exists)
        {
            TempData["SILeaderError"] = $"A-Number '{aNumber}' already exists.";
            return RedirectToAction(nameof(Index), new { search = input.Search });
        }

        _context.SILeaders.Add(new SILeader
        {
            ANumber = aNumber,
            LeaderName = leaderName,
            StoredCourseAssignments = NormalizeAssignments(input.CreateCourseAssignments)
        });

        await ApplyCourseAssignmentsAsync(leaderName, input.CreateCourseAssignments);
        await _context.SaveChangesAsync();
        TempData["SILeaderMessage"] = "SI leader added.";
        return RedirectToAction(nameof(Index), new { search = input.Search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string aNumber, string leaderName, string? courseAssignments, string? search)
    {
        var entity = await _context.SILeaders.FirstOrDefaultAsync(l => l.SILeaderID == id);
        if (entity is null)
        {
            return NotFound();
        }

        var normalizedANumber = (aNumber ?? string.Empty).Trim();
        var normalizedLeaderName = (leaderName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedANumber) || string.IsNullOrWhiteSpace(normalizedLeaderName))
        {
            TempData["SILeaderError"] = "A-Number and SI Leader Name are required.";
            return RedirectToAction(nameof(Index), new { search });
        }

        var aNumberConflict = await _context.SILeaders
            .AnyAsync(l => l.SILeaderID != id && l.ANumber == normalizedANumber);
        if (aNumberConflict)
        {
            TempData["SILeaderError"] = $"A-Number '{normalizedANumber}' already exists.";
            return RedirectToAction(nameof(Index), new { search });
        }

        entity.ANumber = normalizedANumber;
        entity.LeaderName = normalizedLeaderName;
        entity.StoredCourseAssignments = NormalizeAssignments(courseAssignments);
        await ApplyCourseAssignmentsAsync(normalizedLeaderName, courseAssignments);
        await _context.SaveChangesAsync();
        TempData["SILeaderMessage"] = "SI leader updated.";
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? search)
    {
        var entity = await _context.SILeaders.FirstOrDefaultAsync(l => l.SILeaderID == id);
        if (entity is null)
        {
            return NotFound();
        }

        _context.SILeaders.Remove(entity);
        await _context.SaveChangesAsync();
        TempData["SILeaderMessage"] = "SI leader removed.";
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? search)
    {
        var vm = await BuildIndexViewModelAsync(search);
        return View(vm);
    }

    private async Task<SILeaderAdminIndexViewModel> BuildIndexViewModelAsync(string? search)
    {
        var searchValue = (search ?? string.Empty).Trim();
        var leaderQuery = _context.SILeaders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            leaderQuery = leaderQuery.Where(l =>
                l.LeaderName.Contains(searchValue) || l.ANumber.Contains(searchValue));
        }

        var leaders = await leaderQuery
            .OrderBy(l => l.LeaderName)
            .ThenBy(l => l.ANumber)
            .ToListAsync();

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.CourseLeader))
            .Select(c => new
            {
                c.CourseName,
                c.CourseSection,
                c.CourseLeader
            })
            .ToListAsync();

        var groupedByLeader = courses
            .GroupBy(c => c.CourseLeader.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = leaders.Select(leader =>
        {
            groupedByLeader.TryGetValue(leader.LeaderName, out var leaderCourses);
            leaderCourses ??= [];

            var storedAssignments = ParseAssignments(leader.StoredCourseAssignments);

            var distinctCourses = storedAssignments
                .Select(c => c.CourseName.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var distinctSections = storedAssignments
                .Select(c => c.CourseSection.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (distinctCourses.Count == 0)
            {
                distinctCourses = leaderCourses
                    .Select(c => c.CourseName.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }

            if (distinctSections.Count == 0)
            {
                distinctSections = leaderCourses
                    .Select(c => c.CourseSection.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }

            return new SILeaderAdminRowViewModel
            {
                SILeaderID = leader.SILeaderID,
                ANumber = leader.ANumber,
                LeaderName = leader.LeaderName,
                CoursesTeaching = distinctCourses.Count == 0 ? "-" : string.Join(", ", distinctCourses),
                SectionNumbers = distinctSections.Count == 0 ? "-" : string.Join(", ", distinctSections),
                CourseAssignmentsInput = NormalizeAssignments(leader.StoredCourseAssignments)
            };
        }).ToList();

        return new SILeaderAdminIndexViewModel
        {
            Search = searchValue,
            Leaders = rows
        };
    }

    private async Task ApplyCourseAssignmentsAsync(string leaderName, string? rawAssignments)
    {
        if (string.IsNullOrWhiteSpace(leaderName))
        {
            return;
        }

        var parsedAssignments = ParseAssignments(rawAssignments);
        if (parsedAssignments.Count == 0)
        {
            return;
        }

        var courseNames = parsedAssignments
            .Select(a => a.CourseName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var candidateCourses = await _context.Courses
            .Where(c => courseNames.Contains(c.CourseName))
            .ToListAsync();

        foreach (var assignment in parsedAssignments)
        {
            var matches = candidateCourses
                .Where(c => string.Equals(c.CourseName, assignment.CourseName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(c.CourseSection, assignment.CourseSection, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var course in matches)
            {
                course.CourseLeader = leaderName;
            }
        }
    }

    private static List<(string CourseName, string CourseSection)> ParseAssignments(string? rawAssignments)
    {
        if (string.IsNullOrWhiteSpace(rawAssignments))
        {
            return [];
        }

        return rawAssignments
            .Replace("\r", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    return (CourseName: string.Empty, CourseSection: string.Empty);
                }
                return (CourseName: parts[0], CourseSection: parts[1]);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.CourseName) && !string.IsNullOrWhiteSpace(x.CourseSection))
            .Distinct()
            .ToList();
    }

    private static string NormalizeAssignments(string? rawAssignments)
    {
        var parsedAssignments = ParseAssignments(rawAssignments);
        if (parsedAssignments.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            parsedAssignments
                .Select(x => $"{x.CourseName.Trim()}|{x.CourseSection.Trim()}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }
}
