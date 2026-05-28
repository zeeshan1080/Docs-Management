using Microsoft.EntityFrameworkCore;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Locations;
using DocumentManagement.Domain;
using DocumentManagement.Infrastructure.Data;

namespace DocumentManagement.Infrastructure.Services;

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _db;

    public LocationService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<LocationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Locations.AsNoTracking()
            .Where(l => l.RecordStatusLIID == RecordStatus.Active)
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Inactive = false,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationDto>> GetAllForAdminAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Locations.AsNoTracking();
        if (!includeInactive)
            q = q.Where(l => l.RecordStatusLIID == RecordStatus.Active);

        return await q
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Inactive = l.RecordStatusLIID != RecordStatus.Active,
            })
            .ToListAsync(cancellationToken);
    }
}
