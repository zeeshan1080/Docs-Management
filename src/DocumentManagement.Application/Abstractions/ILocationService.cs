using DocumentManagement.Application.Locations;

namespace DocumentManagement.Application.Abstractions;

public interface ILocationService
{
    /// <summary>Active locations only (for registration, sharing pickers, etc.).</summary>
    Task<IReadOnlyList<LocationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin locations list. When <paramref name="includeInactive"/> is false, same as <see cref="GetAllAsync"/> (active only).
    /// </summary>
    Task<IReadOnlyList<LocationDto>> GetAllForAdminAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
