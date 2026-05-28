namespace DocumentManagement.Application.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string RequestedRoleName { get; set; } = "";
    public int RequestedLocationId { get; set; }
}
