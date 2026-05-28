using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DocumentManagement.Api.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Security;

public class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtOptions _jwt;

    public JwtTokenIssuer(IOptions<JwtOptions> jwt) => _jwt = jwt.Value;

    public string CreateAccessToken(string userId, string email, IReadOnlyList<string> roles, ApprovalStatus approvalStatus, string displayName)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.GivenName, displayName),
            new("approval", ((byte)approvalStatus).ToString())
        };
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
