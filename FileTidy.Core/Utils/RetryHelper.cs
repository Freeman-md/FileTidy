namespace FileTidy.Core.Utils;

public static class RetryHelper
{
    public static async Task RetryAsync(
        Func<Task> action, 
        int maxRetries = 3, 
        int delayMs = 200,
        Func<int, Task>? delay = null
        )
    {
        int attempts = 0;
        delay ??= ms => Task.Delay(ms);
        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch when (++attempts < maxRetries)
            {
                await delay(delayMs);
            }
        }
    }
}