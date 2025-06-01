namespace FileTidy.Core.Models;

public class FileMoveResult
{
    public required string OriginalPath { get; init; }
    public required string NewPath { get; init; }
    public FileOperationStatus Status { get; init; }
    public bool Success => Status == FileOperationStatus.Moved;
    public Exception? Error { get; init; }
}
