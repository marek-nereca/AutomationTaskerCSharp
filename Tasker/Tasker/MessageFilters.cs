using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tasker.Models;
using Tasker.Models.Configuration;

namespace Tasker
{
    public static class MessageFilters
    {
        public static bool MeetDayAndDarkFilter(this ISwitchWithNightSelector sw, SensorState sensorState)
        {
            return (!sw.OnlyWhenIsDark || sensorState.IsDark) &&
                   (!sw.OnlyWhenIsNight || !sensorState.IsDayLight);
        }

        public static bool MeetTopicAndPayloadFilter(this MqttSwitch sw, MqttStringMessage message)
        {
            bool res = sw.Topic == message.Topic;
            if (res && sw.PayloadFiltersCombinedByOr.Any())
            {
                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(message.Payload);
                foreach (var payloadFilter in sw.PayloadFiltersCombinedByOr)
                {
                    if (payload.TryGetValue(payloadFilter.Key, out var value) &&
                        value.ToString() == payloadFilter.Value)
                    {
                        return true;
                    }
                }

                return false;
            }

            return res;
        }
    }
}