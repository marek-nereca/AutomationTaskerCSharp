namespace Tasker.Models
{
    public class TurnOnSwitches
    {
        public TasmotaRfSwitchWithTurnOffDelay[] TasmotaRfSwitches { get; set; }
        public MqttSwitchWithTurnOffDelay[] MqttSwitches { get; set; }
    }
}