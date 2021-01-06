using System;

namespace Tasker.Models.Configuration
{
    public class MqttSwitch
    {
        public string Topic { get; set; } = String.Empty;

        public PayloadFilter[] PayloadFiltersCombinedByOr { get; set; } = new PayloadFilter[0];
        
        public HueDevice[] HueDevices { get; set; } = new HueDevice[0];
    }
}