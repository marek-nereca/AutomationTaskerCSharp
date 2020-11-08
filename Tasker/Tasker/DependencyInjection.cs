using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tasker.Models;

namespace Tasker
{
    public static class DependencyInjection
    {
        public static void RegisterDI(this IServiceCollection services, IConfiguration configuration)
        {
            var deviceConfig = new DeviceConfig();
            configuration.GetSection(nameof(DeviceConfig)).Bind(deviceConfig);
            services.AddSingleton(deviceConfig);
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            services.AddSingleton(Log.Logger);

            services.AddSingleton<ActionScheduler>();
            services.AddSingleton<IHueClient, HueClient>();
            services.AddSingleton<IMqttClient, MqttClient>();
        }
    }
}