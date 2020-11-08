using System;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Tasker.Models;

namespace Tasker
{
    public class HueClient : IHueClient
    {
        private class HueDefinition
        {
            public HueBridge HueBridge { get; set; }
            public LocalHueClient HueClient { get; set; }
        }

        private HueDefinition[] _hueDefinitions;

        public HueClient(DeviceConfig deviceConfig)
        {
            _hueDefinitions = deviceConfig.HueBridges.Select(hueBridge => new HueDefinition
            {
                HueBridge = hueBridge,
                HueClient = new LocalHueClient(hueBridge.Host, hueBridge.User)
            }).ToArray();
        }

        public async Task TurnLightOnAsync(HueDevice device)
        {
            var hueDefinition = _hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);
            var cmd = new LightCommand()
            {
                On = true
            };
            await hueDefinition.HueClient.SendCommandAsync(cmd, new[] {device.LightId.ToString()});
        }

        public async Task TurnLightOffAsync(HueDevice device)
        {
            var hueDefinition = _hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);
            var cmd = new LightCommand()
            {
                On = false
            };
            await hueDefinition.HueClient.SendCommandAsync(cmd, new[] {device.LightId.ToString()});
        }

        public async Task SwitchLightAsync(HueDevice device)
        {
            var hueDefinition = _hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);

            var light = await hueDefinition.HueClient.GetLightAsync(device.LightId.ToString());
            if (light == null)
            {
                throw new InvalidOperationException(
                    $"Light with id [{device.LightId}] on bridge [{device.BridgeName}] with host [{hueDefinition.HueBridge.Host}] does not exists.");
            }

            var cmd = new LightCommand()
            {
                On = !light.State.On
            };
            await hueDefinition.HueClient.SendCommandAsync(cmd, new[] {device.LightId.ToString()});
        }
    }
}