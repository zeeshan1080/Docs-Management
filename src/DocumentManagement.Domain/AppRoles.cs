namespace DocumentManagement.Domain;

public static class AppRoles
{
    public const string Management = "Management";
    public const string Stylist = "Stylist";
    public const string MedspaStaff = "Medspa Staff";
    public const string SpaStaff = "Spa Staff";
    public const string Other = "Other";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Management, Stylist, MedspaStaff, SpaStaff, Other
    };
}
