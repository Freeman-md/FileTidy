using FileTidy.Core.Models;
using FileTidy.Data.Sqlite;
using FileTidy.Data.Tests.Builders;

namespace FileTidy.Data.Tests;

[TestFixture]
public class SqliteOperationStoreTests
{
    private SqliteOperationStore _store;
    private string _testDbPath;

    [SetUp]
    public void SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"filetidy-test-{Guid.NewGuid()}.db");

        _store = new SqliteOperationStore(_testDbPath);
    }

    [Test]
    public async Task LogOperationAsync_Should_Insert_Operation_Successfully()
    {
        // Arrange
        FileOperation operation = new FileOperationBuilder().Build();

        // Act
        await _store.LogOperationAsync(operation);
        var result = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.FileName, Is.EqualTo(operation.FileName));
    }

    [Test]
    public void LogOperationAsync_Should_Throw_When_Operation_Is_Null()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _store.LogOperationAsync(null!));
    }

    [Test]
    public async Task LogOperationAsync_Should_Persist_All_Values_Correctly()
    {
        // Arrange
        var operation = new FileOperationBuilder().Build();

        // Act
        await _store.LogOperationAsync(operation);
        var result = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(operation.Id));
        Assert.That(result.FileName, Is.EqualTo(operation.FileName));
        Assert.That(result.OriginalPath, Is.EqualTo(operation.OriginalPath));
        Assert.That(result.NewPath, Is.EqualTo(operation.NewPath));
        Assert.That(result.Status, Is.EqualTo(operation.Status));
        Assert.That(result.SortSessionId, Is.EqualTo(operation.SortSessionId));
        Assert.That(result.Timestamp, Is.EqualTo(operation.Timestamp).Within(TimeSpan.FromSeconds(1)));
    }


    [Test]
    public async Task LogOperationAsync_Should_Store_Timestamp_As_Utc()
    {
        // Arrange
        var operation = new FileOperationBuilder().Build();
        var expected = operation.Timestamp.ToUniversalTime();

        // Act
        await _store.LogOperationAsync(operation);
        var result = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Timestamp, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(1)));
    }
    
    [Test]
    public async Task GetOperationByIdAsync_Should_Return_Operation_When_Id_Exists()
    {
        // Arrange
        var operation = new FileOperationBuilder().Build();
        await _store.LogOperationAsync(operation);

        // Act
        var result = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(operation.Id));
    }

    [Test]
    public async Task GetOperationByIdAsync_Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Act
        var result = await _store.GetOperationByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetOperationByIdAsync_Should_Return_Correct_Values_For_All_Fields()
    {
        // Arrange
        var operation = new FileOperationBuilder().Build();
        await _store.LogOperationAsync(operation);

        // Act
        var result = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(operation.Id));
            Assert.That(result.FileName, Is.EqualTo(operation.FileName));
            Assert.That(result.OriginalPath, Is.EqualTo(operation.OriginalPath));
            Assert.That(result.NewPath, Is.EqualTo(operation.NewPath));
            Assert.That(result.Status, Is.EqualTo(operation.Status));
            Assert.That(result.SortSessionId, Is.EqualTo(operation.SortSessionId));
            Assert.That(result.Timestamp, Is.EqualTo(operation.Timestamp).Within(TimeSpan.FromSeconds(1)));
        });
    }


    [Test]
    public async Task GetOperationsBySessionAsync_Should_Return_Operations_For_Session()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var builder = new FileOperationBuilder();
        var operations = builder.BuildMany(5).Select(op =>
            new FileOperationBuilder()
                .WithSortSessionId(sessionId)
                .WithTimestamp(op.Timestamp)
                .Build()
        ).ToList();

        foreach (var op in operations)
            await _store.LogOperationAsync(op);

        // Act
        var results = (await _store.GetOperationsBySessionAsync(sessionId)).ToList();

        // Assert
        Assert.That(results, Has.Count.EqualTo(5));
        Assert.That(results.Select(x => x.Id), Is.EquivalentTo(operations.Select(x => x.Id)));
    }

    [Test]
    public async Task GetLatestNonRevertedOperationByNewPathAsync_Should_Return_Operation_When_Match_Exists()
    {
        // Arrange
        var fileOperation = new FileOperationBuilder()
            .WithNewPath("C:/Sorted/Images/photo.jpg")
            .WithStatus(FileOperationStatus.Moved)
            .Build();
        
        await _store.LogOperationAsync(fileOperation);
        
        // Act
        var result = await _store.GetLatestNonRevertedOperationByNewPathAsync(fileOperation.NewPath, fileOperation.Status);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(fileOperation.Id));
    }

    [Test]
    public async Task GetLatestNonRevertedOperationByNewPathAsync_Should_Return_Null_When_No_Matching_Status()
    {
        // Arrange
        var fileOperation = new FileOperationBuilder()
            .WithNewPath("C:/Sorted/Docs/report.pdf")
            .WithStatus(FileOperationStatus.Reverted)
            .Build();

        await _store.LogOperationAsync(fileOperation);
        
        // Act
        var result = await _store.GetLatestNonRevertedOperationByNewPathAsync(fileOperation.NewPath, FileOperationStatus.Moved);
        
        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetLatestNonRevertedOperationByNewPathAsync_Should_Return_Latest_When_Multiple_Exist()
    {
        // Arrange
        var path = "C:/Sorted/Videos/movie.mp4";
        var baseTime = DateTime.UtcNow;
        
        var older = new FileOperationBuilder()
            .WithNewPath(path)
            .WithTimestamp(baseTime.AddMinutes(-10))
            .WithStatus(FileOperationStatus.Moved)
            .Build();

        var newer = new FileOperationBuilder()
            .WithNewPath(path)
            .WithTimestamp(baseTime)
            .WithStatus(FileOperationStatus.Moved)
            .Build();
        
        await _store.LogOperationAsync(older);
        await _store.LogOperationAsync(newer);
        
        // Act
        var result = await _store.GetLatestNonRevertedOperationByNewPathAsync(path, FileOperationStatus.Moved);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(newer.Id));
    }

    [Test]
    public async Task GetOperationsBySessionAsync_Should_Return_Empty_When_Session_Has_No_Operations()
    {
        // Arrange
        var unusedSessionId = Guid.NewGuid();

        // Act
        var results = await _store.GetOperationsBySessionAsync(unusedSessionId);

        // Assert
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task GetOperationsBySessionAsync_Should_Return_Results_Ordered_By_Timestamp()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;

        var op1 = new FileOperationBuilder().WithSortSessionId(sessionId).WithTimestamp(baseTime.AddMinutes(-10))
            .Build();
        var op2 = new FileOperationBuilder().WithSortSessionId(sessionId).WithTimestamp(baseTime).Build();
        var op3 = new FileOperationBuilder().WithSortSessionId(sessionId).WithTimestamp(baseTime.AddMinutes(-5))
            .Build();

        var expectedOrder = new[] { op1.Id, op3.Id, op2.Id };

        await _store.LogOperationAsync(op1);
        await _store.LogOperationAsync(op2);
        await _store.LogOperationAsync(op3);

        // Act
        var results = (await _store.GetOperationsBySessionAsync(sessionId)).ToList();

        // Assert
        Assert.That(results.Select(x => x.Id), Is.EqualTo(expectedOrder));
    }

    [Test]
    public async Task GetRecentOperationsAsync_Should_Return_Limited_Number_Of_Recent_Operations()
    {
        // Arrange
        var allOperations = new FileOperationBuilder().BuildMany(10).ToList();

        foreach (var op in allOperations)
            await _store.LogOperationAsync(op);

        // Act
        var results = (await _store.GetRecentOperationsAsync(5)).ToList();

        // Assert
        Assert.That(results, Has.Count.EqualTo(5));
        var expected = allOperations.OrderByDescending(x => x.Timestamp).Take(5).Select(x => x.Id);
        Assert.That(results.Select(x => x.Id), Is.EqualTo(expected));
    }

    [Test]
    public async Task GetRecentOperationsAsync_Should_Return_Empty_When_No_Operations_Exist()
    {
        // Act
        var results = await _store.GetRecentOperationsAsync(5);

        // Assert
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task GetRecentOperationsAsync_Should_Return_Results_Ordered_By_Timestamp_Descending()
    {
        // Arrange
        var operations = new FileOperationBuilder().BuildMany(3).ToList();

        foreach (var op in operations)
            await _store.LogOperationAsync(op);

        // Act
        var results = (await _store.GetRecentOperationsAsync(3)).ToList();

        // Assert
        var expectedOrder = operations.OrderByDescending(x => x.Timestamp).Select(x => x.Id);
        Assert.That(results.Select(x => x.Id), Is.EqualTo(expectedOrder));
    }

    [Test]
    public async Task UpdateOperationStatusAsync_Should_Update_Status_Successfully()
    {
        // Arrange
        var operation = new FileOperationBuilder()
            .WithStatus(FileOperationStatus.Moved)
            .Build();

        await _store.LogOperationAsync(operation);

        // Act
        await _store.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Deleted);
        var updated = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo(FileOperationStatus.Deleted));
    }

    [Test]
    public void UpdateOperationStatusAsync_Should_Not_Throw_When_Operation_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _store.UpdateOperationStatusAsync(nonExistentId, FileOperationStatus.Reverted));
    }

    [Test]
    public async Task UpdateOperationStatusAsync_Should_Allow_Multiple_Status_Changes()
    {
        // Arrange
        var operation = new FileOperationBuilder()
            .WithStatus(FileOperationStatus.Moved)
            .Build();

        await _store.LogOperationAsync(operation);

        // Act
        await _store.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Deleted);
        await _store.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Reverted);
        var updated = await _store.GetOperationByIdAsync(operation.Id);

        // Assert
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo(FileOperationStatus.Reverted));
    }


    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}