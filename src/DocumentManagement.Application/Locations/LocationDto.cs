namespace DocumentManagement.Application.Locations;

public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>True when <c>RecordStatusLIID</c> is inactive (not available for registration/shares).</summary>
    public bool Inactive { get; set; }
}
