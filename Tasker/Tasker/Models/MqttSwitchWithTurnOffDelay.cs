namespace Tasker.Models
{
    public class MqttSwitchWithTurnOffDelay : MqttSwitch
    {
        public int TurnOffDelay { get; set; } = 0;
    }
}