using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Tasker.Models;
using Tasker.Models.Configuration;

namespace Tasker
{
    public class TaskerService : BackgroundService
    {
        private const string RfTopic = "tasmota/tele/sonoff/RESULT";

        private readonly DeviceConfig _deviceConfig;

        private readonly IHueClient _hueClient;

        private readonly IMqttClient _mqttClient;

        private readonly ISensorStateUpdater _sensorStateUpdater;

        private readonly ILogger _log;

        private readonly ActionScheduler _actionScheduler;

        public TaskerService(DeviceConfig deviceConfig, IHueClient hueClient, IMqttClient mqttClient, ILogger log,
            ActionScheduler actionScheduler, ISensorStateUpdater sensorStateUpdater)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
            _hueClient = hueClient ?? throw new ArgumentNullException(nameof(hueClient));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _actionScheduler = actionScheduler ?? throw new ArgumentNullException(nameof(actionScheduler));
            _sensorStateUpdater = sensorStateUpdater ?? throw new ArgumentNullException(nameof(sensorStateUpdater));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _sensorStateUpdater.Start(stoppingToken);
            var sensorStates = _sensorStateUpdater.CreateObservableDataStream();
            var messages = await _mqttClient.CreateMessageStreamAsync(stoppingToken);

            var messagesWithState = messages.Zip(sensorStates.MostRecent(new SensorState()),
                (message, state) => new MqttStringMessageWithState
                {
                    Message = message,
                    SensorState = state
                });
            
            var rfMessagesWithState = SelectRfMessages(messagesWithState);
            rfMessagesWithState.Subscribe(rf => { _log.Information("Rf Message received {@message}", rf); });

            var mqMessagesWithState = SelectMqMessages(messagesWithState);
            mqMessagesWithState.Subscribe(mq => { _log.Information("Mq message received {@message}", mq); });

            var hueDevicesToSwitchMq = mqMessagesWithState.SelectMany(mq =>
                _deviceConfig.SimpleSwitches.MqttSwitches.Where(mqSwitch => mqSwitch.Topic == mq.Message.Topic).SelectMany(mqSwitch => mqSwitch.HueDevices));
            var hueDevicesToSwitchRf = rfMessagesWithState.SelectMany(rf =>
                _deviceConfig.SimpleSwitches.TasmotaRfSwitches
                    .Where(rfSwitch => rfSwitch.RfData == rf.Message.RfReceived.Data)
                    .SelectMany(rfSwitch => rfSwitch.HueDevices));
            var hueDevicesToSwitch = hueDevicesToSwitchMq.Merge(hueDevicesToSwitchRf);
            hueDevicesToSwitch.Subscribe(async hueDevice =>
            {
                await _hueClient.SwitchDeviceAsync(hueDevice);
            });

            var turnOnSwitchMessages = rfMessagesWithState.Where(rfMessage =>
                _deviceConfig.OnSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.Message.RfReceived.Data));

            turnOnSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.OnSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.Message.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device =>
                {
                    await _hueClient.TurnDeviceOnAsync(device);
                    if (rfSwitch.TurnOffDelay > 0)
                    {
                        var task = _actionScheduler.RegisterAction(
                            $"turnOff_{device.BridgeName}_{device.Id}_{device.IsGroup}",
                            async () => { await _hueClient.TurnDeviceOffAsync(device); },
                            TimeSpan.FromMilliseconds(rfSwitch.TurnOffDelay), stoppingToken);

                        task.Forget();
                    }
                });
            });

            var turnOffSwitchMessages = rfMessagesWithState.Where(rfMessage =>
                _deviceConfig.OffSwitches.TasmotaRfSwitches.Select(rfSwitch => rfSwitch.RfData)
                    .Contains(rfMessage.Message.RfReceived.Data));
            turnOffSwitchMessages.Subscribe(received =>
            {
                var rfSwitch =
                    _deviceConfig.OffSwitches.TasmotaRfSwitches.Single(rfs =>
                        rfs.RfData == received.Message.RfReceived.Data);

                rfSwitch.HueDevices.ToList().ForEach(async device => { await _hueClient.TurnDeviceOffAsync(device); });
            });
        }

        private static IObservable<TasmotaRfMessageWithState> SelectRfMessages(IObservable<MqttStringMessageWithState> messages)
        {
            var rfMessages = messages.Where(msgWrapper => msgWrapper.Message.Topic == RfTopic).SelectMany(
                message =>
                    Observable.Return(new TasmotaRfMessageWithState {
                        Message = JsonConvert.DeserializeObject<TasmotaRfMessage>(message.Message.Payload),
                        SensorState = message.SensorState
                    }));
            return rfMessages;
        }

        private static IObservable<MqttStringMessageWithState> SelectMqMessages(IObservable<MqttStringMessageWithState> messages)
        {
            var mqMessages = messages.Where(msgWrapper => msgWrapper.Message.Topic != RfTopic);
            return mqMessages;
        }
    }
}