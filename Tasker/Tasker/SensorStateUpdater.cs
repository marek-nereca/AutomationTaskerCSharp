using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Q42.HueApi;
using Serilog;
using Tasker.Models;
using Tasker.Models.Configuration;

namespace Tasker
{
        public class SensorStateUpdater : ISensorStateUpdater
        {
            public event ISensorStateUpdater.SensorStateReceivedDelegate? SensorStateReceived;

            private Task? _task;
            
            private readonly LocalHueClient _hueClient;
            private readonly int _darkSensorId;
            private readonly int _daySensorId;
            private readonly int _intervalMs;
            

            public SensorStateUpdater(HueSensorUpdater hueSensorUpdaterConfig, HueBridge[] hueBridges)
            {
                const int minInterval = 100;
                if (hueSensorUpdaterConfig.IntervalMs <= minInterval)
                {
                    throw new ArgumentException($"Interval must be greater than {minInterval}");
                }
                var hueBridge = hueBridges.SingleOrDefault(hb => hb.Name == hueSensorUpdaterConfig.BridgeName);
                if (hueBridge == null)
                {
                    throw new ArgumentException($"Hue bridge with name [{hueSensorUpdaterConfig.BridgeName}] was not found in configuration.");
                }
                
                _darkSensorId = hueSensorUpdaterConfig.DarkSensorId;
                _daySensorId = hueSensorUpdaterConfig.DaySensorId;
                _intervalMs = hueSensorUpdaterConfig.IntervalMs;
                _hueClient = new LocalHueClient(hueBridge.Host, hueBridge.User);
            }

            public void Start(CancellationToken cancellationToken)
            {
                if (_task != null)
                {
                    throw new InvalidOperationException("Updater already started.");
                }
                
                _task = Task.Factory.StartNew((async o =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var sensors = await _hueClient.GetSensorsAsync();
                            var state = new SensorState
                            {
                                IsDark = sensors.Single(arr => arr.Id == _darkSensorId.ToString())?.State.Dark ?? false,
                                IsDayLight = sensors.Single(arr => arr.Id == _daySensorId.ToString())?.State.Daylight ?? false
                            };
                            Log.Logger.Information("Actual state {@state}", state);
                            SensorStateReceived?.Invoke(state);
                        }
                        catch (Exception e)
                        {
                            Log.Logger.Error(e, "Get state failed");
                        }

                        await Task.Delay(_intervalMs, cancellationToken);
                    }
                }), cancellationToken, TaskCreationOptions.LongRunning);
            }
        }
}