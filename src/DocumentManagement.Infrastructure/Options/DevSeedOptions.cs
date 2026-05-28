namespace DocumentManagement.Infrastructure.Options;

public class DevSeedOptions
{
    public const string SectionName = "DevSeed";
    public bool Enabled { get; set; }
    public string AdminEmail { get; set; } = "admin@local.test";
    public string AdminPassword { get; set; } = "Admin123!";
}
