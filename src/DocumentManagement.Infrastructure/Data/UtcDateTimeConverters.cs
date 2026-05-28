using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DocumentManagement.Infrastructure.Data;

/// <summary>
/// SQL Server <c>datetime2</c> has no time zone; we store UTC instants and materialize as <see cref="DateTimeKind.Utc"/>
/// so JSON and clients see consistent UTC (e.g. ISO-8601 with <c>Z</c>).
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Local
                ? DateTime.SpecifyKind(v.ToUniversalTime(), DateTimeKind.Unspecified)
                : DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

internal sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter()
        : base(
            v => !v.HasValue
                ? null
                : v.Value.Kind == DateTimeKind.Local
                    ? DateTime.SpecifyKind(v.Value.ToUniversalTime(), DateTimeKind.Unspecified)
                    : DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified),
            v => !v.HasValue ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
    {
    }
}
