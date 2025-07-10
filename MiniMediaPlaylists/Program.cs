using CliFx;
using DbUp;
using MiniMediaPlaylists.Jobs;
using Quartz;
using Quartz.Impl;

namespace MiniMediaPlaylists;

public class Program
{
    public static string[] ConsoleArguments { get; private set; }

    public static async Task Main(string[] args)
    {
        ConsoleArguments = args;

        string? cronExpression = Environment.GetEnvironmentVariable("CRON");
        string? commandEnvironmentValue = Environment.GetEnvironmentVariable("COMMAND");
        
        if (!string.IsNullOrWhiteSpace(commandEnvironmentValue) && args?.Length == 0)
        {
            ConsoleArguments = [ commandEnvironmentValue ];
        }

        string? connectionString = GetConnectionString(args);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Connection String is required");
            return;
        }
        
        var upgradeEngine = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithExecutionTimeout(TimeSpan.FromMinutes(15))
            .WithScriptsFromFileSystem("./DbScripts")
            .LogToConsole()
            .Build();

        var result = upgradeEngine.PerformUpgrade();
        
        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            await CreateSchedulerAsync(cronExpression);
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        else
        {
            try
            {
                await new CliApplicationBuilder()
                    .AddCommandsFromThisAssembly()
                    .Build()
                    .RunAsync(ConsoleArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    static string? GetConnectionString(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--connection-string")
            {
                return args.Skip(i + 1).FirstOrDefault();
            }
        }

        return Environment.GetEnvironmentVariable("CONNECTIONSTRING");
    }

    static async Task CreateSchedulerAsync(string cronExpression)
    {
        var factory = new StdSchedulerFactory();
        var scheduler = await factory.GetScheduler();
        await scheduler.Start();

        var job = JobBuilder.Create<CronJob>()
            .WithIdentity("cronJob", "group")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("trigger", "group")
            .WithCronSchedule(cronExpression)
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }
}