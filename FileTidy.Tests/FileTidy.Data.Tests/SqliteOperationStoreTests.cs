using FIleTidy.Data.Sqlite;

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
        
    }

    [Test]
    public async Task LogOperationAsync_Should_Throw_When_Operation_Is_Null()
    {
        
    }

    [Test]
    public async Task LogOperationAsync_Should_Persist_All_Values_Correctly()
    {
        
    }

    [Test]
    public async Task LogOperationAsync_Should_Store_Timestamp_As_Utc()
    {
        
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}