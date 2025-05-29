namespace FileTidy.Core.Interfaces;

public interface IFileTidyingService
{
    void SortDirectory(string directoryPath, ISortReporter? reporter = null);
}