namespace DocumentManagement.Domain;

public enum RegistrationRequestStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
