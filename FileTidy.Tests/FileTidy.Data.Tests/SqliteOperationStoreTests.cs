using FileTidy.Core.Models;
using FIleTidy.Data.Sqlite;
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


    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}