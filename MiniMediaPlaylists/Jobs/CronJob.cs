using CliFx;
using Quartz;

namespace MiniMediaPlaylists.Jobs;

[DisallowConcurrentExecution]
public class CronJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(Program.ConsoleArguments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}