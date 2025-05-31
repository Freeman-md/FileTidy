namespace FileTidy.Core.Models;

public class FileOperation
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string OriginalPath { get; set; }
    public required string NewPath { get; set; }
    public FileOperationStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid SortSessionId { get; set; }
}