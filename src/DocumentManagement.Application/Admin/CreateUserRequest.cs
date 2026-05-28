namespace DocumentManagement.Application.Admin;

public class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string RoleId { get; set; } = "";
    public int LocationId { get; set; }
}
