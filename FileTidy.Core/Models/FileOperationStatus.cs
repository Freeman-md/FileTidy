namespace FileTidy.Core.Models;

public enum FileOperationStatus
{
    Moved,
    Reverted,
    Deleted,
    Failed,
    Skipped
}