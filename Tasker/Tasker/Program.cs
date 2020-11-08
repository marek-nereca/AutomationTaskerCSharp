using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Tasker.Models;

namespace Tasker
{
    class Program
    {
        static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(((context, configuration) =>
                {
                    IHostEnvironment env = context.HostingEnvironment;
                    configuration
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true); 
                    IConfigurationRoot configurationRoot = configuration.Build();

                }))
                .ConfigureServices((hostContext, services) =>
                {
                    services.RegisterDI(hostContext.Configuration);
                    services.AddHostedService<TaskerService>();
                })
                .UseSerilog();

    }
}