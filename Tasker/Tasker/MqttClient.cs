using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Serilog;
using Tasker.Models;

namespace Tasker
{
    public class MqttClient : IMqttClient
    {
        private readonly DeviceConfig _deviceConfig;
        private readonly ILogger _log;

        public MqttClient(DeviceConfig deviceConfig, ILogger log)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<IObservable<MqttStringMessage>> CreateMessageStreamAsync(CancellationToken token)
        {
            var client = await CreateMqttClient(_deviceConfig.MqttBroker.Host);
            var messagesRaw = new Subject<MqttApplicationMessageReceivedEventArgs>();
            client.UseApplicationMessageReceivedHandler(eventArgs => { messagesRaw.OnNext(eventArgs); });

            var messages = messagesRaw.SelectMany(mr => Observable.Return(new MqttStringMessage
            {
                Topic = mr.ApplicationMessage.Topic,
                Payload = Encoding.UTF8.GetString(mr.ApplicationMessage.Payload)
            }));

            var logSubscription = messages.Subscribe(message =>
            {
                _log.Debug("Mqtt received {@message}", message);
            });
            
            token.Register(async () =>
            {
                logSubscription.Dispose();
                await client.StopAsync();
                client.Dispose();
            });

            return messages;
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