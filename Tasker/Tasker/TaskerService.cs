using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Tasker.Models;

namespace Tasker
{
    public class TaskerService : BackgroundService
    {
        private const string RfTopic = "tasmota/tele/sonoff/RESULT";
        
        private readonly DeviceConfig _deviceConfig;

        private readonly IHueClient _hueClient;

        private readonly IMqttClient _mqttClient;
        
        private readonly ILogger _log;

        public TaskerService(DeviceConfig deviceConfig, IHueClient hueClient, IMqttClient mqttClient, ILogger log)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
            _hueClient = hueClient ?? throw new ArgumentNullException(nameof(hueClient));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var messages = await _mqttClient.CreateMessageStreamAsync(stoppingToken);

            var rfMessages = SelectRfMessages(messages);

            rfMessages.Subscribe(rf =>
            {
                _log.Information("Rf Message received {@message}", rf.RfReceived);
            });

            var simpleSwitchMessages = rfMessages.Where(rfMessage =>
                _deviceConfig.SimpleSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.RfReceived.Data));

            simpleSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.SimpleSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device => { await _hueClient.SwitchDeviceAsync(device); });
            });


            var turnOnSwitchMessages = rfMessages.Where(rfMessage =>
                _deviceConfig.OnSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.RfReceived.Data));

            turnOnSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.OnSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device => { await _hueClient.TurnDeviceOnAsync(device); });
            });

            var turnOffSwitchMessages = rfMessages.Where(rfMessage =>
                _deviceConfig.OffSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.RfReceived.Data));
            turnOffSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.OffSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device => { await _hueClient.TurnDeviceOffAsync(device); });
            });
        }


        private static IObservable<TasmotaRfMessage> SelectRfMessages(IObservable<MqttStringMessage> messages)
        {
            var rfMessages = messages.Where(message => message.Topic == RfTopic).SelectMany(
                message =>
                    Observable.Return(JsonConvert.DeserializeObject<TasmotaRfMessage>(message.Payload)));
            return rfMessages;
        }
        
        private static IObservable<MqttStringMessage> SelectMqMessages(IObservable<MqttStringMessage> messages)
        {
            var mqMessages = messages.Where(message => message.Topic != RfTopic);
            return mqMessages;
        }
    }
}