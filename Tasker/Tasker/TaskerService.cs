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
        private readonly IMqttClient _mqttClient;

        private readonly ISensorStateUpdater _sensorStateUpdater;

        private readonly ILogger _log;

        private readonly IActionProcessor _actionProcessor;

        private readonly MessageProcessor _messageProcessor;

        private IDisposable? _subscription;

        public TaskerService(ILogger log, IMqttClient mqttClient,
            ISensorStateUpdater sensorStateUpdater, IActionProcessor actionProcessor, MessageProcessor messageProcessor)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _sensorStateUpdater = sensorStateUpdater ?? throw new ArgumentNullException(nameof(sensorStateUpdater));
            _actionProcessor = actionProcessor ?? throw new ArgumentNullException(nameof(actionProcessor));
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _sensorStateUpdater.Start(stoppingToken);
            var sensorStates = _sensorStateUpdater.CreateObservableDataStream();
            var messages = await _mqttClient.CreateMessageStreamAsync(stoppingToken);

            var messagesWithState = MessageProcessor.CombineMqMessagesWithSensorState(messages, sensorStates);
            var messageProcessingPipeline = _messageProcessor.CreateMessageProcessingPipeline(stoppingToken, messagesWithState);
            _subscription = messageProcessingPipeline.Subscribe(action =>
            {
                action.Process(_actionProcessor);
            });
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _subscription?.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}