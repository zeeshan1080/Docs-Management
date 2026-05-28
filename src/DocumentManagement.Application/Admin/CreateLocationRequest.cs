namespace DocumentManagement.Application.Admin;

public class CreateLocationRequest
{
    public string Name { get; set; } = "";

    /// <summary>When true, sets <c>RecordStatusLIID</c> to inactive.</summary>
    public bool Inactive { get; set; }
}
