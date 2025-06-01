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

        var op1 = new FileOperationBuilder().WithSortSessionId(sessionId).WithTimestamp(baseTime.AddMinutes(-10)).Build();
        var op2 = new FileOperationBuilder().WithSortSessionId(sessionId).WithTimestamp(baseTime).Build();
        var op3 = new FileOperationBuilder().WithSortSessionId(sessionId).WithTimestamp(baseTime.AddMinutes(-5)).Build();

        var expectedOrder = new[] { op1.Id, op3.Id, op2.Id };

        await _store.LogOperationAsync(op1);
        await _store.LogOperationAsync(op2);
        await _store.LogOperationAsync(op3);

        // Act
        var results = (await _store.GetOperationsBySessionAsync(sessionId)).ToList();

        // Assert
        Assert.That(results.Select(x => x.Id), Is.EqualTo(expectedOrder));
    }



    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}