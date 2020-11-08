using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Q42.HueApi;
using Tasker.Models;

namespace Tasker
{
    public class TaskerService : BackgroundService
    {
        private readonly DeviceConfig _deviceConfig;

        public TaskerService(DeviceConfig deviceConfig)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mqttServerHost = _deviceConfig.MqttBroker.Host;
            var mqttClient = await CreateMqttClient(mqttServerHost);
            var messagesRaw = new Subject<MqttApplicationMessageReceivedEventArgs>();
            mqttClient.UseApplicationMessageReceivedHandler(eventArgs => { messagesRaw.OnNext(eventArgs); });

            var messagesString = messagesRaw.SelectMany(mr => Observable.Return(new MqttStringMessage
            {
                Topic = mr.ApplicationMessage.Topic,
                Payload = Encoding.UTF8.GetString(mr.ApplicationMessage.Payload)
            }));

            messagesString.Subscribe(message =>
            {
                Console.WriteLine(message.Topic);
                Console.WriteLine(message.Payload);
            });

            var rfMessages = messagesString.Where(message => message.Topic == "tasmota/tele/sonoff/RESULT").SelectMany(
                message =>
                    Observable.Return(JsonConvert.DeserializeObject<TasmotaRfMessage>(message.Payload)));

            rfMessages.Subscribe(rf => { Console.WriteLine(rf.RfReceived.Data); });

            var hueDefinitions = _deviceConfig.HueBridges.Select(hueBridge => new
            {
                HueBridge = hueBridge,
                HueClient = new LocalHueClient(hueBridge.Host, hueBridge.User)
            }).ToArray();

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
                    var hueDefinition = hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);

                    var light = await hueDefinition.HueClient.GetLightAsync(device.LightId.ToString());
                    if (light == null)
                    {
                        throw new InvalidOperationException($"Light with id [{device.LightId}] on bridge [{device.BridgeName}] with host [{hueDefinition.HueBridge.Host}] does not exists.");
                    }
                    var cmd = new LightCommand()
                    {
                        On = !light.State.On
                    };
                    await hueDefinition.HueClient.SendCommandAsync(cmd, new []{device.LightId.ToString()});
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
                    var hueDefinition = hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);
                    var cmd = new LightCommand()
                    {
                        On = true
                    };
                    await hueDefinition.HueClient.SendCommandAsync(cmd, new []{device.LightId.ToString()});
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
                    var hueDefinition = hueDefinitions.Single(hd => hd.HueBridge.Name == device.BridgeName);
                    var cmd = new LightCommand()
                    {
                        On = false
                    };
                    await hueDefinition.HueClient.SendCommandAsync(cmd, new []{device.LightId.ToString()});
                });
            });
        }

        private static async Task<IManagedMqttClient> CreateMqttClient(string mqttServerHost)
        {
            var opt = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(
                    new MqttClientOptionsBuilder().WithClientId("csharpTasker").WithTcpServer(mqttServerHost).Build()
                ).Build();
            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            await mqttClient.SubscribeAsync("#");
            await mqttClient.StartAsync(opt);
            return mqttClient;
        }
    }
}