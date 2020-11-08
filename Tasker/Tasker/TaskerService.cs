using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Tasker.Models;

namespace Tasker
{
    public class TaskerService : BackgroundService
    {
        private readonly DeviceConfig _deviceConfig;

        private readonly IHueClient _hueClient;

        private readonly IMqttClient _mqttClient;

        public TaskerService(DeviceConfig deviceConfig, IHueClient hueClient, IMqttClient mqttClient)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
            _hueClient = hueClient ?? throw new ArgumentNullException(nameof(hueClient));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var messages = await _mqttClient.CreateMessageStreamAsync(stoppingToken);
            
            var rfMessages = messages.Where(message => message.Topic == "tasmota/tele/sonoff/RESULT").SelectMany(
                message =>
                    Observable.Return(JsonConvert.DeserializeObject<TasmotaRfMessage>(message.Payload)));

            rfMessages.Subscribe(rf => { Console.WriteLine(rf.RfReceived.Data); });

            var simpleSwitchMessages = rfMessages.Where(rfMessage =>
                _deviceConfig.SimpleSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.RfReceived.Data));

            simpleSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.SimpleSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device =>
                {
                    await _hueClient.SwitchLightAsync(device);
                });
            });
            
            
            var turnOnSwitchMessages = rfMessages.Where(rfMessage =>
                _deviceConfig.OnSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.RfReceived.Data));

            turnOnSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.OnSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device =>
                {
                    await _hueClient.TurnLightOnAsync(device);
                });
            });
            
            var turnOffSwitchMessages = rfMessages.Where(rfMessage =>
                _deviceConfig.OffSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.RfReceived.Data));
            turnOffSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.OffSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device =>
                {
                    await _hueClient.TurnLightOffAsync(device);
                });
            });
        }
    }
}