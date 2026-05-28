using Microsoft.AspNetCore.Identity;

namespace DocumentManagement.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public DateTime CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public int RecordStatusLIID { get; set; } = 1;
}
