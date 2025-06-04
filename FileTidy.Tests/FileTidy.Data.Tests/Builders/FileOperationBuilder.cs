using FileTidy.Core.Models;

namespace FileTidy.Data.Tests.Builders;

public class FileOperationBuilder
{
    private FileOperation _fileOperation;

    public FileOperationBuilder()
    {
        _fileOperation = new FileOperation()
        {
            Id = Guid.NewGuid(),
            FileName = Guid.NewGuid().ToString(),
            NewPath = Guid.NewGuid().ToString(),
            OriginalPath = Guid.NewGuid().ToString(),
            Status = FileOperationStatus.Moved,
            SortSessionId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };
    }

    public FileOperationBuilder WithId(Guid id)
    {
        _fileOperation.Id = id;
        return this;
    }
    
    public FileOperationBuilder WithFileName(string fileName)
    {
        _fileOperation.FileName = fileName;
        return this;
    }
    
    public FileOperationBuilder WithNewPath(string path)
    {
        _fileOperation.NewPath = path.Replace('\\', '/');
        return this;
    }

    public FileOperationBuilder WithOriginalPath(string path)
    {
        _fileOperation.OriginalPath = path.Replace('\\', '/');
        return this;
    }

    
    public FileOperationBuilder WithStatus(FileOperationStatus status)
    {
        _fileOperation.Status = status;
        return this;
    }
    
    public FileOperationBuilder WithTimestamp(DateTime timestamp)
    {
        _fileOperation.Timestamp = timestamp;
        return this;
    }
    
    public FileOperationBuilder WithSortSessionId(Guid sortSessionId)
    {
        _fileOperation.SortSessionId = sortSessionId;
        return this;
    }

    public FileOperation Build()
    {
        return new FileOperation()
        {
            Id = _fileOperation.Id,
            FileName = _fileOperation.FileName,
            NewPath = _fileOperation.NewPath,
            OriginalPath = _fileOperation.OriginalPath,
            Status = _fileOperation.Status,
            SortSessionId = _fileOperation.SortSessionId,
            Timestamp = _fileOperation.Timestamp,
        };
    }

    public IEnumerable<FileOperation> BuildMany(int limit = 10)
    {
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        foreach (var number in Enumerable.Range(0, limit))
        {
            yield return new FileOperation
            {
                Id = Guid.NewGuid(),
                FileName = $"file_{number}.txt",
                OriginalPath = $"/downloads/file_{number}.txt",
                NewPath = $"/downloads/Images/file_{number}.txt",
                Status = FileOperationStatus.Moved,
                SortSessionId = sessionId,
                Timestamp = now.AddSeconds(number)
            };
        }
    }

}