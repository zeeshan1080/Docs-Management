namespace DocumentManagement.Infrastructure.Identity;

public static class UserDisplayName
{
    public static string? Format(ApplicationUser? u)
    {
        if (u == null) return null;
        var name = $"{u.FirstName} {u.LastName}".Trim();
        return string.IsNullOrEmpty(name) ? u.Email : name;
    }
}
