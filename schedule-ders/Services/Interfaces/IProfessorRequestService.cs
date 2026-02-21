using schedule_ders.Contracts.Api.V1.Requests;
using schedule_ders.Contracts.Api.V1.Responses;

namespace schedule_ders.Services.Interfaces;

public interface IProfessorRequestService
{
    Task<SiRequestSummaryDto> CreateRequestAsync(CreateSiRequestDto input, string createdByUserId);
    Task<ProfessorRequestStatusLookupResult> GetOwnedStatusAsync(int requestId, string userId);
}

public class ProfessorRequestStatusLookupResult
{
    public bool Exists { get; set; }
    public bool IsOwner { get; set; }
    public RequestStatusDto? Status { get; set; }
}
