using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Serilog.Core;
using Tasker.Models;
using Tasker.Models.ActionMessages;
using Tasker.Models.Configuration;

namespace Tasker.Tests
{
    public class CombineMessagesWithStateTests
    {
        private MessageProcessor _messageProcessor;

        [SetUp]
        public void Setup()
        {
            _messageProcessor = new MessageProcessor(Logger.None, new DeviceConfig
            {
                HueBridges = new[]
                {
                    new HueBridge
                    {
                        Host = "testhost",
                        Name = "testHostName",
                        User = "usr"
                    }
                },

                SimpleSwitches = new Switches
                {
                    MqttSwitches = new[]
                    {
                        new MqttSwitch
                        {
                            Topic = "/mqtt/on",
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 1
                                }
                            }
                        },
                    }
                }
            }, new ActionScheduler(Logger.None));
        }

        [Test]
        public async Task Test_Combine()
        {
            var sensorState = new SensorState();
            var mqttStringMessage = new MqttStringMessage
            {
                Payload = "1",
                Topic = "/mqtt/on"
            };
            var inputMessages = new[] {mqttStringMessage};

            await ExecuteTest(sensorState, inputMessages, list =>
            {
                var action = list.Single();
                Assert.IsInstanceOf<SwitchDevice>(action);
                var turnOnAction = (SwitchDevice) action;
                Assert.AreEqual(1, turnOnAction.HueDevice.Id);
                Assert.AreEqual("testHostName", turnOnAction.HueDevice.BridgeName);
                Assert.AreEqual(false, turnOnAction.HueDevice.IsGroup);
            });
        }

        private async Task ExecuteTest(SensorState sensorState, MqttStringMessage[] inputMessages,
            Action<List<IActionMessage>> assertAction)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var sensorStates = new Subject<SensorState>();
            var stringMessages = new Subject<MqttStringMessage>();

            var messagesWithState = MessageProcessor.CombineMqMessagesWithSensorState(stringMessages, sensorStates);
            var processingPipeline = _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token, messagesWithState);

            List<IActionMessage> actionMessages = new List<IActionMessage>();
            var subscription = processingPipeline.Subscribe(action =>
            {
                Console.WriteLine($"msg: {action}");
                actionMessages.Add(action);
            });

            sensorStates.OnNext(sensorState);
            foreach (var inputMessage in inputMessages)
            {
                stringMessages.OnNext(inputMessage);
            }

            stringMessages.OnCompleted();
            sensorStates.OnCompleted();
            cancellationTokenSource.Cancel();
            await processingPipeline.ToArray();
            subscription.Dispose();

            assertAction(actionMessages);
        }
    }
}