using System;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Serilog;
using Tasker.Models.Configuration;

namespace Tasker
{
    public class HueClient : IHueClient
    {
        private class HueDefinition
        {
            public HueBridge HueBridge { get; set; }
            public LocalHueClient HueClient { get; set; }
        }

        private readonly HueDefinition[] _hueDefinitions;

        private ILogger _log;

        public HueClient(DeviceConfig deviceConfig, ILogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _hueDefinitions = deviceConfig.HueBridges.Select(hueBridge => new HueDefinition
            {
                HueBridge = hueBridge,
                HueClient = new LocalHueClient(hueBridge.Host, hueBridge.User)
            }).ToArray();
        }

        public async Task TurnDeviceOnAsync(HueDevice device)
        {
            var hueDefinition = _hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);
            var cmd = new LightCommand()
            {
                On = true
            };
            
            _log.Information("Turning on {@device}", device);
            if (device.IsGroup)
            {
                await hueDefinition.HueClient.SendGroupCommandAsync(cmd, device.Id.ToString());
            }
            else
            {
                await hueDefinition.HueClient.SendCommandAsync(cmd, new[] {device.Id.ToString()});
            }
        }

        public async Task TurnDeviceOffAsync(HueDevice device)
        {
            var hueDefinition = _hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);
            var cmd = new LightCommand()
            {
                On = false
            };
            
            _log.Information("Turning off {@device}", device);
            if (device.IsGroup)
            {
                await hueDefinition.HueClient.SendGroupCommandAsync(cmd, device.Id.ToString());
            }
            else
            {
                await hueDefinition.HueClient.SendCommandAsync(cmd, new[] {device.Id.ToString()});
            }
        }

        public async Task SwitchDeviceAsync(HueDevice device)
        {
            var hueDefinition = _hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);

            if (device.IsGroup)
            {
                await SwitchGroupAsync(device, hueDefinition);
            }
            else
            {
                await SwitchLightAsync(device, hueDefinition);
            }
        }

        private async Task SwitchLightAsync(HueDevice device, HueDefinition hueDefinition)
        {
            var light = await hueDefinition.HueClient.GetLightAsync(device.Id.ToString());
            if (light == null)
            {
                throw new InvalidOperationException(
                    $"Light with id [{device.Id}] on bridge [{device.BridgeName}] with host [{hueDefinition.HueBridge.Host}] does not exists.");
            }

            var cmd = new LightCommand()
            {
                On = !light.State.On
            };
            
            _log.Information("Switching state {on} to {@device}", cmd.On, device);
            await hueDefinition.HueClient.SendCommandAsync(cmd, new[] {device.Id.ToString()});
        }

        private async Task SwitchGroupAsync(HueDevice device, HueDefinition hueDefinition)
        {
            var group = await hueDefinition.HueClient.GetGroupAsync(device.Id.ToString());
            if (group == null)
            {
                throw new InvalidOperationException(
                    $"Group with id [{device.Id}] on bridge [{device.BridgeName}] with host [{hueDefinition.HueBridge.Host}] does not exists.");
            }

            var cmd = new LightCommand()
            {
                On = !group.State.AnyOn
            };
            
            _log.Information("Switching state {on} to {@device}", cmd.On, device);
            await hueDefinition.HueClient.SendGroupCommandAsync(cmd, device.Id.ToString());
        }
    }
}