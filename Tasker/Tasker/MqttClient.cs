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
using Tasker.Models.Configuration;

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
            var messages = new Subject<MqttStringMessage>();
            client.UseApplicationMessageReceivedHandler(eventArgs =>
            {
                var msg = new MqttStringMessage
                {
                    Topic = eventArgs.ApplicationMessage.Topic,
                    Payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)
                };
                _log.Debug("Mqtt received {@message}", msg);
                messages.OnNext(msg);
            });

            token.Register(async () =>
            {
                await client.StopAsync();
                client.Dispose();
            });

            return messages;
        }
        
        private static async Task<IManagedMqttClient> CreateMqttClient(string mqttServerHost)
        {
            var opt = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(
                    new MqttClientOptionsBuilder()
                        .WithCleanSession()
                        .WithTcpServer(mqttServerHost)
                        .Build()
                ).Build();
            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            await mqttClient.SubscribeAsync("#");
            await mqttClient.StartAsync(opt);
            return mqttClient;
        }
    }
}