using DocumentManagement.Application.Email;

namespace DocumentManagement.Application.Abstractions;

/// <summary>Builds branded HTML + subjects for transactional email.</summary>
public interface IEmailTemplates
{
    EmailContent RegistrationPendingManagement(
        string employeeEmail,
        string employeeDisplayName,
        string requestedRoleName,
        string requestedLocationName,
        string reviewRegistrationsUrl);

    /// <summary>Confirmation to the employee after self-registration (pending approval).</summary>
    EmailContent RegistrationSubmitted(
        string employeeDisplayName,
        string requestedRoleName,
        string requestedLocationName);

    EmailContent RegistrationApproved(
        string employeeDisplayName,
        string assignedRoleName,
        string assignedLocationName,
        string signInUrl);

    EmailContent RegistrationRejected(string employeeDisplayName, string? notes);

    EmailContent ForgotPassword(string resetUrl, string? recipientDisplayName);

    EmailContent PasswordChangedConfirmation(string? recipientDisplayName);

    EmailContent FolderAccessGranted(
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl);

    EmailContent FolderAccessRevoked(
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl);

    EmailContent DocumentAccessGranted(
        string fileName,
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl);

    EmailContent DocumentAccessRevoked(
        string fileName,
        string folderName,
        string actionByDisplayName,
        string accessDescription,
        string documentsUrl);

    /// <summary>Management created a user account (password communicated out of band).</summary>
    EmailContent AccountCreatedByAdmin(
        string employeeDisplayName,
        string assignedRoleName,
        string assignedLocationName,
        string signInUrl,
        string createdByDisplayName);
}
