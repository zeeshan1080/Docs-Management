using Microsoft.AspNetCore.Identity;

namespace DocumentManagement.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public byte ApprovalStatus { get; set; }

    public DateTime CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public int RecordStatusLIID { get; set; } = 1;
}
