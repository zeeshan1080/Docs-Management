using DocumentManagement.Domain;

namespace DocumentManagement.Application.Registration;

public class RegistrationRequestDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
    public string RequestedRoleId { get; set; } = "";
    public string RequestedRoleName { get; set; } = "";
    public int RequestedLocationId { get; set; }
    public string RequestedLocationName { get; set; } = "";
    public RegistrationRequestStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
}
