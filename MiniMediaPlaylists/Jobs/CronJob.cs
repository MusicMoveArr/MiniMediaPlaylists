using CliFx;
using Quartz;

namespace MiniMediaPlaylists.Jobs;

[DisallowConcurrentExecution]
public class CronJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        foreach (string command in Program.ConsoleArguments)
        {
            try
            {
                await new CliApplicationBuilder()
                    .AddCommandsFromThisAssembly()
                    .Build()
                    .RunAsync([command]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}