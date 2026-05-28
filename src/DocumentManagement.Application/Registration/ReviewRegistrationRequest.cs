using DocumentManagement.Domain;

namespace DocumentManagement.Application.Registration;

public class ReviewRegistrationRequest
{
    public bool Approve { get; set; }
    public string? AssignedRoleId { get; set; }
    public int? AssignedLocationId { get; set; }
    public string? Notes { get; set; }
}
