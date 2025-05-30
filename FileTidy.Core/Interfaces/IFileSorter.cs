using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileSorter
{
    TidyingResult Sort(List<string> filesToSort);
    TidyingResult Sort(List<string> filesToSort, CancellationToken cancellationToken);
}