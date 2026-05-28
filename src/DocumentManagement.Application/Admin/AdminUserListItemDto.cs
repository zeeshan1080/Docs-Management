namespace DocumentManagement.Application.Admin;

public class AdminUserListItemDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public byte ApprovalStatus { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public int? PrimaryLocationId { get; set; }
    public string? PrimaryLocationName { get; set; }
}
