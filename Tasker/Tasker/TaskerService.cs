using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Tasker.Models;
using Tasker.Models.ActionMessages;
using Tasker.Models.Configuration;

namespace Tasker
{
    public class TaskerService : BackgroundService
    {
        private const string RfTopic = "tasmota/tele/sonoff/RESULT";

        private readonly DeviceConfig _deviceConfig;

        private readonly IMqttClient _mqttClient;

        private readonly ISensorStateUpdater _sensorStateUpdater;

        private readonly ILogger _log;

        private readonly ActionScheduler _actionScheduler;

        private readonly IActionProcessor _actionProcessor;

        public TaskerService(DeviceConfig deviceConfig, IMqttClient mqttClient, ILogger log,
            ActionScheduler actionScheduler, ISensorStateUpdater sensorStateUpdater, IActionProcessor actionProcessor)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _actionScheduler = actionScheduler ?? throw new ArgumentNullException(nameof(actionScheduler));
            _sensorStateUpdater = sensorStateUpdater ?? throw new ArgumentNullException(nameof(sensorStateUpdater));
            _actionProcessor = actionProcessor ?? throw new ArgumentNullException(nameof(actionProcessor));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _sensorStateUpdater.Start(stoppingToken);
            var sensorStates = _sensorStateUpdater.CreateObservableDataStream();
            var messages = await _mqttClient.CreateMessageStreamAsync(stoppingToken);

            var actions = MessageProcess(stoppingToken, messages, sensorStates);

            actions.Subscribe(action =>
            {
                action.Process(_actionProcessor);
            });
        }

        private IObservable<IActionMessage> MessageProcess(CancellationToken stoppingToken, IObservable<MqttStringMessage> messages, IObservable<SensorState> sensorStates)
        {
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

            var switchActions = ResolveSwitchRequests(mqMessagesWithState, rfMessagesWithState);

            var turnOffActionsAfterDelay = new Subject<IActionMessage>();
            var turnOnActions = ResolveTurnOnRequests(stoppingToken, rfMessagesWithState, turnOffActionsAfterDelay);

            var turnOffActions = ResolveTurnOnRequests(rfMessagesWithState);

            var actions = turnOffActions.Merge(switchActions).Merge(turnOnActions).Merge(turnOffActionsAfterDelay);
            
            return actions;
        }

        private IObservable<IActionMessage> ResolveTurnOnRequests(IObservable<TasmotaRfMessageWithState> rfMessagesWithState)
        {
            var turnOffDevices = rfMessagesWithState.SelectMany(rf =>
                _deviceConfig.OffSwitches.TasmotaRfSwitches
                    .Where(rfSwitch => rfSwitch.RfData == rf.Message.RfReceived.Data)
                    .SelectMany(rfSwitch => rfSwitch.HueDevices));
            var turnOffActions = turnOffDevices.Select(device => new TurnOffDevice(device) as IActionMessage);
            return turnOffActions;
        }

        private IObservable<IActionMessage> ResolveTurnOnRequests(CancellationToken stoppingToken, IObservable<TasmotaRfMessageWithState> rfMessagesWithState,
            Subject<IActionMessage> turnOffActionsAfterDelay)
        {
            var turnOnSwitches = rfMessagesWithState.SelectMany(rfm =>
                _deviceConfig.OnSwitches.TasmotaRfSwitches.Where(rfSwitch =>
                    rfm.Message.RfReceived.Data == rfSwitch.RfData &&
                    (!rfSwitch.OnlyWhenIsDark || rfm.SensorState.IsDark) &&
                    (!rfSwitch.OnlyWhenIsNight || !rfm.SensorState.IsDayLight)));
            var turnOnDevices = turnOnSwitches.SelectMany(sw => sw.HueDevices);
            var turnOnActions = turnOnDevices.Select(device => new TurnOnDevice(device) as IActionMessage);

            var turnOffAfterDelay = turnOnSwitches.Where(sw => sw.TurnOffDelay > 0 && sw.HueDevices != null).SelectMany(sw =>
                sw.HueDevices.Select(device => new
                {
                    sw.TurnOffDelay,
                    Device = device
                }));
            turnOffAfterDelay.Subscribe(definition =>
            {
                var task = _actionScheduler.RegisterAction(
                    $"turnOff_{definition.Device.BridgeName}_{definition.Device.Id}_{definition.Device.IsGroup}",
                    () => { turnOffActionsAfterDelay.OnNext(new TurnOffDevice(definition.Device)); },
                    TimeSpan.FromMilliseconds(definition.TurnOffDelay), stoppingToken);
                task.Forget();
            });
            return turnOnActions;
        }

        private IObservable<IActionMessage> ResolveSwitchRequests(IObservable<MqttStringMessageWithState> mqMessagesWithState, IObservable<TasmotaRfMessageWithState> rfMessagesWithState)
        {
            var hueDevicesToSwitchMq = mqMessagesWithState.SelectMany(mq =>
                _deviceConfig.SimpleSwitches.MqttSwitches.Where(mqSwitch => mqSwitch.Topic == mq.Message.Topic)
                    .SelectMany(mqSwitch => mqSwitch.HueDevices));
            var hueDevicesToSwitchRf = rfMessagesWithState.SelectMany(rf =>
                _deviceConfig.SimpleSwitches.TasmotaRfSwitches
                    .Where(rfSwitch => rfSwitch.RfData == rf.Message.RfReceived.Data)
                    .SelectMany(rfSwitch => rfSwitch.HueDevices));
            var hueDevicesToSwitch = hueDevicesToSwitchMq.Merge(hueDevicesToSwitchRf);
            var switchActions = hueDevicesToSwitch.Select(device => new SwitchDevice(device) as IActionMessage);
            return switchActions;
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