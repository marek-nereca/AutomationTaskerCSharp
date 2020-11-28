namespace Tasker.Models.Configuration
{
    public class TurnOnSwitches
    {
        public TasmotaRfSwitchWithTurnOffDelay[] TasmotaRfSwitches { get; set; } = new TasmotaRfSwitchWithTurnOffDelay[0];
        public MqttSwitchWithTurnOffDelay[] MqttSwitches { get; set; } = new MqttSwitchWithTurnOffDelay[0];
    }
}