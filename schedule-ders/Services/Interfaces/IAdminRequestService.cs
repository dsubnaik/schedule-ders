using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Contracts.Api.V1.Responses;

namespace schedule_ders.Services.Interfaces;

public interface IAdminRequestService
{
    Task<PagedResultDto<AdminRequestListItemDto>> GetRequestsAsync(
        string? status,
        string? course,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);

    Task<SiRequestSummaryDto?> UpdateStatusAsync(int requestId, UpdateRequestStatusDto input);

    Task<RemoveAdminRequestResult> RemoveRequestAsync(int requestId);
}

public enum RemoveAdminRequestResult
{
    NotFound = 0,
    NotAllowed = 1,
    Removed = 2
}
