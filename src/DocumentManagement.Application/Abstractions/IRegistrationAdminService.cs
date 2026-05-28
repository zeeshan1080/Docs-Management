using DocumentManagement.Application.Registration;

namespace DocumentManagement.Application.Abstractions;

public interface IRegistrationAdminService
{
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegistrationRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> ReviewAsync(string managerUserId, int requestId, ReviewRegistrationRequest review, CancellationToken cancellationToken = default);
}
