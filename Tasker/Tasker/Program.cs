using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
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
                    var deviceConfig = new DeviceConfig();
                    hostContext.Configuration.GetSection(nameof(DeviceConfig)).Bind(deviceConfig);
                    services.AddSingleton(deviceConfig);
                    
                    services.AddHostedService<TaskerService>();
                });

    }
}