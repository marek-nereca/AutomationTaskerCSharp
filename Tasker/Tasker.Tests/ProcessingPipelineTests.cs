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
    public class ProcessingPipelineTests
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
                    },
                    TasmotaRfSwitches = new[]
                    {
                        new TasmotaRfSwitch
                        {
                            RfData = "devid2",
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 2
                                }
                            }
                        }
                    }
                },
                OffSwitches = new Switches
                {
                    MqttSwitches = new[]
                    {
                        new MqttSwitch
                        {
                            Topic = "/mqtt/off",
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 3
                                }
                            }
                        },
                    },
                    TasmotaRfSwitches = new[]
                    {
                        new TasmotaRfSwitch
                        {
                            RfData = "offDevid4",
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 4
                                }
                            }
                        }
                    }
                },

                OnSwitches = new TurnOnSwitches()
                {
                    MqttSwitches = new[]
                    {
                        new MqttSwitchWithTurnOffDelay()
                        {
                            Topic = "/mqtt/on",
                            TurnOffDelayMs = 0,
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 5
                                }
                            }
                        },
                    },
                    TasmotaRfSwitches = new[]
                    {
                        new TasmotaRfSwitchWithTurnOffDelay()
                        {
                            RfData = "onDevid6",
                            TurnOffDelayMs = 0,
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 6
                                }
                            }
                        },
                        new TasmotaRfSwitchWithTurnOffDelay()
                        {
                            RfData = "onDevid8",
                            TurnOffDelayMs = 100,
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 8
                                }
                            }
                        },
                        new TasmotaRfSwitchWithTurnOffDelay()
                        {
                            RfData = "onDevid10",
                            TurnOffDelayMs = 0,
                            OnlyWhenIsNight = true,
                            HueDevices = new[]
                            {
                                new HueDevice
                                {
                                    BridgeName = "testHostName",
                                    Id = 10
                                }
                            }
                        }
                    }
                }
            }, new ActionScheduler(Logger.None));
        }

        [Test]
        public async Task Test_MqSimpleSwitch()
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload = "1",
                        Topic = "/mqtt/on"
                    },
                    SensorState = new SensorState()
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            cancellationTokenSource.Cancel();
            var result = await messageProcessingPipeline.SingleAsync();
            Assert.IsInstanceOf<SwitchDevice>(result);
            var switchAction = (SwitchDevice) result;
            Assert.AreEqual(1, switchAction.HueDevice.Id);
        }

        [Test]
        public void Test_MqOffSwitch_DoesNotWorkYet()
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload = "1",
                        Topic = "/mqtt/off"
                    },
                    SensorState = new SensorState()
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            cancellationTokenSource.Cancel();

            var any = messageProcessingPipeline.ToEnumerable().Any();
            Assert.IsFalse(any);

            // var result = await messageProcessingPipeline.SingleAsync();
            // Assert.IsInstanceOf<TurnOffDevice>(result);
            // var offDevice = (TurnOffDevice) result;
            // Assert.AreEqual(1, offDevice.HueDevice.Id);
        }

        [Test]
        public async Task Test_RfSimpleSwitch()
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload =
                            "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"devid2\", \"RfKey\": \"None\"}}",
                        Topic = MessageProcessor.RfTopic
                    },
                    SensorState = new SensorState()
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            cancellationTokenSource.Cancel();
            var result = await messageProcessingPipeline.SingleAsync();
            Assert.IsInstanceOf<SwitchDevice>(result);
            var switchAction = (SwitchDevice) result;
            Assert.AreEqual(2, switchAction.HueDevice.Id);
        }

        [Test]
        public async Task Test_RfOff()
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload =
                            "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"offDevid4\", \"RfKey\": \"None\"}}",
                        Topic = MessageProcessor.RfTopic
                    },
                    SensorState = new SensorState()
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            cancellationTokenSource.Cancel();
            var result = await messageProcessingPipeline.SingleAsync();
            Assert.IsInstanceOf<TurnOffDevice>(result);
            var turnOffDevice = (TurnOffDevice) result;
            Assert.AreEqual(4, turnOffDevice.HueDevice.Id);
        }

        [Test]
        public async Task Test_RfOn()
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload =
                            "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"onDevid6\", \"RfKey\": \"None\"}}",
                        Topic = MessageProcessor.RfTopic
                    },
                    SensorState = new SensorState()
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            cancellationTokenSource.Cancel();
            var result = await messageProcessingPipeline.SingleAsync();
            Assert.IsInstanceOf<TurnOnDevice>(result);
            var turnOnDevice = (TurnOnDevice) result;
            Assert.AreEqual(6, turnOnDevice.HueDevice.Id);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_RfOn_OnlyInNight(bool isDayLight)
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload =
                            "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"onDevid10\", \"RfKey\": \"None\"}}",
                        Topic = MessageProcessor.RfTopic
                    },
                    SensorState = new SensorState
                    {
                        IsDayLight = isDayLight
                    }
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            cancellationTokenSource.Cancel();

            if (isDayLight)
            {
                var result = await messageProcessingPipeline.FirstOrDefaultAsync();
                Assert.IsNull(result);
            }
            else
            {
                var result = await messageProcessingPipeline.SingleAsync();
                Assert.IsInstanceOf<TurnOnDevice>(result);
                var turnOnDevice = (TurnOnDevice) result;
                Assert.AreEqual(10, turnOnDevice.HueDevice.Id);
            }
        }

        [Test]
        public async Task Test_RfOnWithDelay()
        {
            MqttStringMessageWithState[] messages = new[]
            {
                new MqttStringMessageWithState
                {
                    Message = new MqttStringMessage()
                    {
                        Payload =
                            "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"onDevid8\", \"RfKey\": \"None\"}}",
                        Topic = MessageProcessor.RfTopic
                    },
                    SensorState = new SensorState()
                }
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token,
                    messages.ToObservable());
            List<(IActionMessage, DateTime)> received = new List<(IActionMessage, DateTime)>();
            messageProcessingPipeline.Subscribe(action =>
            {
                var actionAndTime = (action, DateTime.UtcNow);
                received.Add(actionAndTime);
            });
            cancellationTokenSource.CancelAfter(200);
            await messageProcessingPipeline.LastAsync();

            Assert.AreEqual(2, received.Count);

            var onActionAndTime = received.First();
            Assert.IsInstanceOf<TurnOnDevice>(onActionAndTime.Item1);

            var offActionAndTime = received.Last();
            Assert.IsInstanceOf<TurnOffDevice>(offActionAndTime.Item1);

            var delay = offActionAndTime.Item2.Subtract(onActionAndTime.Item2);
            var delta = Math.Abs(100 - delay.TotalMilliseconds);
            Assert.Less(delta, 20);
        }

        [Test]
        public async Task Test_RfOnWithDelay_TwoOnMessages()
        {
            var firstMsg = new MqttStringMessageWithState
            {
                Message = new MqttStringMessage()
                {
                    Payload =
                        "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"onDevid8\", \"RfKey\": \"None\"}}",
                    Topic = MessageProcessor.RfTopic
                },
                SensorState = new SensorState()
            };

            var secondMsg = new MqttStringMessageWithState
            {
                Message = new MqttStringMessage()
                {
                    Payload =
                        "{\"RfReceived\": {\"Sync\": 7560, \"Low\": 250, \"High\": 710, \"Data\": \"onDevid8\", \"RfKey\": \"None\"}}",
                    Topic = MessageProcessor.RfTopic
                },
                SensorState = new SensorState()
            };

            Subject<MqttStringMessageWithState> subject = new Subject<MqttStringMessageWithState>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var messageProcessingPipeline =
                _messageProcessor.CreateMessageProcessingPipeline(cancellationTokenSource.Token, subject);

            List<(IActionMessage, DateTime)> received = new List<(IActionMessage, DateTime)>();
            messageProcessingPipeline.Subscribe(action =>
            {
                var actionAndTime = (action, DateTime.UtcNow);
                received.Add(actionAndTime);
            });
            subject.OnNext(firstMsg);
            await Task.Delay(80);
            subject.OnNext(secondMsg);

            subject.OnCompleted();
            cancellationTokenSource.CancelAfter(200);

            await messageProcessingPipeline.LastAsync();

            Assert.AreEqual(3, received.Count);

            var onActionAndTime = received.First();
            Assert.IsInstanceOf<TurnOnDevice>(onActionAndTime.Item1);

            var on2ActionAndTime = received.Skip(1).First();
            Assert.IsInstanceOf<TurnOnDevice>(on2ActionAndTime.Item1);

            var offActionAndTime = received.Last();
            Assert.IsInstanceOf<TurnOffDevice>(offActionAndTime.Item1);

            var delay = offActionAndTime.Item2.Subtract(onActionAndTime.Item2);
            var delta = Math.Abs(180 - delay.TotalMilliseconds);
            Assert.Less(delta, 20);
        }
    }
}