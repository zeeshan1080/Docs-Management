namespace DocumentManagement.Application.Documents;

public class InitChunkUploadRequestDto
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long TotalSize { get; set; }
    public int TotalChunks { get; set; }
}

public class InitChunkUploadResponseDto
{
    public Guid SessionId { get; set; }
}

public class CompleteChunkUploadRequestDto
{
    public Guid SessionId { get; set; }
}
