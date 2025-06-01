namespace FileTidy.Core.Models;

public class FileMoveResult
{
    public string OriginalPath { get; init; }
    public string NewPath { get; init; }
    public FileOperationStatus Status { get; init; }
    public bool Success => Status == FileOperationStatus.Moved;
    public Exception? Error { get; init; }
}
