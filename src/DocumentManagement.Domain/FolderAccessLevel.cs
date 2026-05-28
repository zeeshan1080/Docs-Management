namespace DocumentManagement.Domain;

/// <summary>Folder share permission: browse and download only, or upload and full file list inheritance.</summary>
public enum FolderAccessLevel : byte
{
    ViewOnly = 1,
    FullAccess = 2,
}
