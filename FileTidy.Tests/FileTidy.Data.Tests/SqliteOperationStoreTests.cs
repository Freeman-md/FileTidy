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

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}