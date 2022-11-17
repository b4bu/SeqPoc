using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile($"appsettings.json");
                var configuration = configurationBuilder.Build();
                services.AddHostedService<Worker>();
                services.AddLogging(configure =>
                configure
                    //.AddConsole(options => options.IncludeScopes = true)
                    .AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        //options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    })
                    .AddSeq(configuration.GetSection("Seq")));
            });
}

internal class Worker : BackgroundService
{
    ILogger<Worker> logger;
    private int executionCount = 0;

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (logger.BeginScope(new MyDictionary<string, object>
        {
            // ["Application"] = "Seqpoc", // No need. Added as property on the API key at seq server.
            ["Foo"] = 42,
            ["Bar"] = 43
        }))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Zzzzzz
                await Task.Delay(1000, stoppingToken);
                logger.LogInformation("2Worker: LogInformation {Count}", Interlocked.Increment(ref executionCount));
            }
        }
    }
}

public class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    public MyDictionary() : base() { }
    public MyDictionary(int capacity) : base(capacity) { }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this); // Here's why. The Console logger provider calls Dictionary.ToString() and we want to content to be displayed.
    }
}