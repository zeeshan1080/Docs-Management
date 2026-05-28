namespace DocumentManagement.Application.Admin;

public class UpdateUserRequest
{
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string RoleId { get; set; } = "";
    public int LocationId { get; set; }
    public byte ApprovalStatus { get; set; }

    /// <summary>When set, replaces the user's password (admin reset).</summary>
    public string? NewPassword { get; set; }
}
