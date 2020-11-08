using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IHueClient, HueClient>();
            services.AddSingleton<IMqttClient, MqttClient>();
        }
    }
}