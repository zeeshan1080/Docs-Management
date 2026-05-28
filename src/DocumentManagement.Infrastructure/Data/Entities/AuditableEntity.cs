namespace DocumentManagement.Infrastructure.Data.Entities;

/// <summary>
/// Standard row audit + record status (lookup id, default 1 = active).
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public int RecordStatusLIID { get; set; } = 1;
}
