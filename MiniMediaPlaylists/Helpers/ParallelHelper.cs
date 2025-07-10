namespace MiniMediaPlaylists.Helpers;

public static class ParallelHelper
{
    public static async Task ForEachAsync<T>(
        IEnumerable<T> source,
        int maxDegreeOfParallelism,
        Func<T, Task> asyncAction)
    {
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                await asyncAction(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}