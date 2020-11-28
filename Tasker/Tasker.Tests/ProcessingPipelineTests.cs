using System.Reactive.Linq;
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
    }
}