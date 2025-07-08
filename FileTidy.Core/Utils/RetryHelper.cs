namespace FileTidy.Core.Utils;

public static class RetryHelper
{
    public static async Task RetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 200)
    {
        int attempts = 0;
        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch when (++attempts < maxRetries)
            {
                await Task.Delay(delayMs);
            }
        }
    }
}