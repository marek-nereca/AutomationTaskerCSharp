namespace Tasker.Models.Configuration
{
    public class Switches
    {
        public TasmotaRfSwitch[] TasmotaRfSwitches { get; set; } = new TasmotaRfSwitch[0];
        public MqttSwitch[] MqttSwitches { get; set; } = new MqttSwitch[0];
    }
}