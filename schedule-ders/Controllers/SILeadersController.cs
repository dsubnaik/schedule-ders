using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;
using schedule_ders.ViewModels;

namespace schedule_ders.Controllers;

[Authorize(Roles = "Admin")]
public class SILeadersController : Controller
{
    private const string ColumnVisibilityCookieName = "si-leader-column-visibility";
    private const string ExportLabelsCookieName = "si-leader-export-labels";
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
    public async Task<IActionResult> Create(SILeaderAdminIndexViewModel input, IFormCollection form)
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

        var customFieldIds = await _context.SILeaderCustomFields
            .Select(f => f.SILeaderCustomFieldId)
            .ToListAsync();
        var customValues = ExtractCustomFieldValues(form, customFieldIds);

        _context.SILeaders.Add(new SILeader
        {
            ANumber = aNumber,
            LeaderName = leaderName,
            StoredCourseAssignments = NormalizeAssignments(input.CreateCourseAssignments),
            CustomValues = customValues
                .Select(entry => new SILeaderCustomValue
                {
                    SILeaderCustomFieldId = entry.Key,
                    Value = entry.Value
                })
                .ToList()
        });

        await ApplyCourseAssignmentsAsync(leaderName, input.CreateCourseAssignments);
        await _context.SaveChangesAsync();
        TempData["SILeaderMessage"] = "SI leader added.";
        return RedirectToAction(nameof(Index), new { search = input.Search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string aNumber, string leaderName, string? courseAssignments, string? search, IFormCollection form)
    {
        var entity = await _context.SILeaders
            .Include(l => l.CustomValues)
            .FirstOrDefaultAsync(l => l.SILeaderID == id);
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

        var customFieldIds = await _context.SILeaderCustomFields
            .Select(f => f.SILeaderCustomFieldId)
            .ToListAsync();
        var customValues = ExtractCustomFieldValues(form, customFieldIds);

        entity.ANumber = normalizedANumber;
        entity.LeaderName = normalizedLeaderName;
        entity.StoredCourseAssignments = NormalizeAssignments(courseAssignments);
        SyncCustomValues(entity, customValues);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField(string createCustomFieldName, string? search)
    {
        var fieldName = (createCustomFieldName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            TempData["SILeaderError"] = "Column name is required.";
            return RedirectToAction(nameof(Index), new { search });
        }

        var exists = await _context.SILeaderCustomFields
            .AnyAsync(f => f.Name == fieldName);
        if (exists)
        {
            TempData["SILeaderError"] = $"Column '{fieldName}' already exists.";
            return RedirectToAction(nameof(Index), new { search });
        }

        var nextDisplayOrder = await _context.SILeaderCustomFields
            .Select(f => (int?)f.DisplayOrder)
            .MaxAsync() ?? 0;

        _context.SILeaderCustomFields.Add(new SILeaderCustomField
        {
            Name = fieldName,
            DisplayOrder = nextDisplayOrder + 1
        });

        await _context.SaveChangesAsync();
        TempData["SILeaderMessage"] = $"Column '{fieldName}' added.";
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteField(int id, string? search)
    {
        var field = await _context.SILeaderCustomFields
            .FirstOrDefaultAsync(f => f.SILeaderCustomFieldId == id);
        if (field is null)
        {
            return NotFound();
        }

        _context.SILeaderCustomFields.Remove(field);
        await _context.SaveChangesAsync();
        TempData["SILeaderMessage"] = $"Column '{field.Name}' removed.";
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? search)
    {
        var vm = await BuildIndexViewModelAsync(search);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateVisibility(bool showANumber, bool showLeaderName, bool showCoursesTeaching, bool showSectionNumbers, string? search)
    {
        var hiddenColumns = new List<string>();
        if (!showANumber)
        {
            hiddenColumns.Add("ANumber");
        }

        if (!showLeaderName)
        {
            hiddenColumns.Add("LeaderName");
        }

        if (!showCoursesTeaching)
        {
            hiddenColumns.Add("CoursesTeaching");
        }

        if (!showSectionNumbers)
        {
            hiddenColumns.Add("SectionNumbers");
        }

        Response.Cookies.Append(
            ColumnVisibilityCookieName,
            string.Join(",", hiddenColumns),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true
            });

        TempData["SILeaderMessage"] = "Column visibility updated.";
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetVisibility(string? search)
    {
        Response.Cookies.Delete(ColumnVisibilityCookieName);
        TempData["SILeaderMessage"] = "Default columns restored.";
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateExportLabels(
        string? exportTitle,
        string? exportANumberLabel,
        string? exportLeaderNameLabel,
        string? exportCoursesTeachingLabel,
        string? exportSectionNumbersLabel,
        string? search)
    {
        var labels = new ExportLabelSettings(
            ExportTitle: CleanLabel(exportTitle, "SI Leaders"),
            ExportANumberLabel: CleanLabel(exportANumberLabel, "A-Number"),
            ExportLeaderNameLabel: CleanLabel(exportLeaderNameLabel, "SI Leader Name"),
            ExportCoursesTeachingLabel: CleanLabel(exportCoursesTeachingLabel, "Courses Teaching"),
            ExportSectionNumbersLabel: CleanLabel(exportSectionNumbersLabel, "Section Numbers"));

        var cookieValue = string.Join("||", new[]
        {
            Uri.EscapeDataString(labels.ExportTitle),
            Uri.EscapeDataString(labels.ExportANumberLabel),
            Uri.EscapeDataString(labels.ExportLeaderNameLabel),
            Uri.EscapeDataString(labels.ExportCoursesTeachingLabel),
            Uri.EscapeDataString(labels.ExportSectionNumbersLabel)
        });

        Response.Cookies.Append(
            ExportLabelsCookieName,
            cookieValue,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true
            });

        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetExportLabels(string? search)
    {
        Response.Cookies.Delete(ExportLabelsCookieName);
        return RedirectToAction(nameof(Index), new { search });
    }

    private async Task<SILeaderAdminIndexViewModel> BuildIndexViewModelAsync(string? search)
    {
        var searchValue = (search ?? string.Empty).Trim();
        var visibility = GetBuiltInColumnVisibility();
        var exportLabels = GetExportLabelSettings();
        var customFields = await _context.SILeaderCustomFields
            .AsNoTracking()
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .Select(f => new SILeaderCustomFieldViewModel
            {
                SILeaderCustomFieldId = f.SILeaderCustomFieldId,
                Name = f.Name
            })
            .ToListAsync();

        var leaderQuery = _context.SILeaders
            .AsNoTracking()
            .Include(l => l.CustomValues)
            .AsQueryable();

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
                CourseAssignmentsInput = NormalizeAssignments(leader.StoredCourseAssignments),
                CustomFieldValues = customFields.ToDictionary(
                    field => field.SILeaderCustomFieldId,
                    field => leader.CustomValues
                        .FirstOrDefault(value => value.SILeaderCustomFieldId == field.SILeaderCustomFieldId)?.Value ?? string.Empty)
            };
        }).ToList();

        return new SILeaderAdminIndexViewModel
        {
            Search = searchValue,
            Leaders = rows,
            CustomFields = customFields,
            CreateCustomFieldValues = customFields.ToDictionary(field => field.SILeaderCustomFieldId, _ => string.Empty),
            ShowANumber = visibility.ShowANumber,
            ShowLeaderName = visibility.ShowLeaderName,
            ShowCoursesTeaching = visibility.ShowCoursesTeaching,
            ShowSectionNumbers = visibility.ShowSectionNumbers,
            ExportTitle = exportLabels.ExportTitle,
            ExportANumberLabel = exportLabels.ExportANumberLabel,
            ExportLeaderNameLabel = exportLabels.ExportLeaderNameLabel,
            ExportCoursesTeachingLabel = exportLabels.ExportCoursesTeachingLabel,
            ExportSectionNumbersLabel = exportLabels.ExportSectionNumbersLabel
        };
    }

    private (bool ShowANumber, bool ShowLeaderName, bool ShowCoursesTeaching, bool ShowSectionNumbers) GetBuiltInColumnVisibility()
    {
        var hidden = Request.Cookies[ColumnVisibilityCookieName] ?? string.Empty;
        var hiddenSet = hidden
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return (
            ShowANumber: !hiddenSet.Contains("ANumber"),
            ShowLeaderName: !hiddenSet.Contains("LeaderName"),
            ShowCoursesTeaching: !hiddenSet.Contains("CoursesTeaching"),
            ShowSectionNumbers: !hiddenSet.Contains("SectionNumbers"));
    }

    private ExportLabelSettings GetExportLabelSettings()
    {
        var defaults = new ExportLabelSettings(
            ExportTitle: "SI Leaders",
            ExportANumberLabel: "A-Number",
            ExportLeaderNameLabel: "SI Leader Name",
            ExportCoursesTeachingLabel: "Courses Teaching",
            ExportSectionNumbersLabel: "Section Numbers");

        var raw = Request.Cookies[ExportLabelsCookieName];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaults;
        }

        var parts = raw.Split("||");
        if (parts.Length != 5)
        {
            return defaults;
        }

        return new ExportLabelSettings(
            ExportTitle: CleanLabel(Uri.UnescapeDataString(parts[0]), defaults.ExportTitle),
            ExportANumberLabel: CleanLabel(Uri.UnescapeDataString(parts[1]), defaults.ExportANumberLabel),
            ExportLeaderNameLabel: CleanLabel(Uri.UnescapeDataString(parts[2]), defaults.ExportLeaderNameLabel),
            ExportCoursesTeachingLabel: CleanLabel(Uri.UnescapeDataString(parts[3]), defaults.ExportCoursesTeachingLabel),
            ExportSectionNumbersLabel: CleanLabel(Uri.UnescapeDataString(parts[4]), defaults.ExportSectionNumbersLabel));
    }

    private static string CleanLabel(string? value, string fallback)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }

    private sealed record ExportLabelSettings(
        string ExportTitle,
        string ExportANumberLabel,
        string ExportLeaderNameLabel,
        string ExportCoursesTeachingLabel,
        string ExportSectionNumbersLabel);

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

    private static Dictionary<int, string> ExtractCustomFieldValues(IFormCollection form, IEnumerable<int> allowedFieldIds)
    {
        var allowed = allowedFieldIds.ToHashSet();
        var values = new Dictionary<int, string>();

        foreach (var key in form.Keys.Where(k => k.StartsWith("customField_", StringComparison.OrdinalIgnoreCase)))
        {
            var suffix = key["customField_".Length..];
            if (!int.TryParse(suffix, out var fieldId) || !allowed.Contains(fieldId))
            {
                continue;
            }

            var value = (form[key].ToString() ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values[fieldId] = value;
            }
        }

        return values;
    }

    private void SyncCustomValues(SILeader entity, Dictionary<int, string> submittedValues)
    {
        var existingByField = entity.CustomValues
            .ToDictionary(value => value.SILeaderCustomFieldId);

        foreach (var customValue in entity.CustomValues.ToList())
        {
            if (!submittedValues.ContainsKey(customValue.SILeaderCustomFieldId))
            {
                _context.SILeaderCustomValues.Remove(customValue);
            }
        }

        foreach (var entry in submittedValues)
        {
            if (existingByField.TryGetValue(entry.Key, out var existing))
            {
                existing.Value = entry.Value;
                continue;
            }

            entity.CustomValues.Add(new SILeaderCustomValue
            {
                SILeaderCustomFieldId = entry.Key,
                Value = entry.Value
            });
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
