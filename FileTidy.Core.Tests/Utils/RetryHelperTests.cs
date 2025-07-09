using FileTidy.Core.Utils;
using Xunit;

namespace FileTidy.Core.Tests.Utils;

public class RetryHelperTests
{
    [Fact]
    public async Task RetryAsync_ExecutesSuccessfullyWithoutRetry()
    {
        // Arrange
        int attempts = 0;

        async Task Action()
        {
            attempts++;
            await Task.CompletedTask;
        }

        // Act
        await RetryHelper.RetryAsync(Action, delay: _ => Task.CompletedTask);

        // Assert
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task RetryAsync_RetriesOnFailureAndEventuallySucceeds()
    {
        // Arrange
        int attempts = 0;

        async Task Action()
        {
            attempts++;
            if (attempts < 3)
                throw new InvalidOperationException("Fail first 2 attempts");
        }

        // Act
        await RetryHelper.RetryAsync(Action, delay: _ => Task.CompletedTask);

        // Assert
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RetryAsync_ThrowsAfterMaxRetries()
    {
        // Arrange
        int attempts = 0;

        async Task Action()
        {
            attempts++;
            throw new Exception("Always fail");
        }

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            RetryHelper.RetryAsync(Action, maxRetries: 3, delayMs: 10, delay: _ => Task.CompletedTask));

        Assert.Equal(3, attempts);
        Assert.Equal("Always fail", ex.Message);
    }
}
