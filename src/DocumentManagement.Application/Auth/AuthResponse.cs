using DocumentManagement.Domain;

namespace DocumentManagement.Application.Auth;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserId { get; set; } = "";
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public ApprovalStatus ApprovalStatus { get; set; }
    public string DisplayName { get; set; } = "";
}
