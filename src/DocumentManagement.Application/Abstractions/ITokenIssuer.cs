using DocumentManagement.Domain;

namespace DocumentManagement.Application.Abstractions;

public interface ITokenIssuer
{
    string CreateAccessToken(string userId, string email, IReadOnlyList<string> roles, ApprovalStatus approvalStatus, string displayName);
}
