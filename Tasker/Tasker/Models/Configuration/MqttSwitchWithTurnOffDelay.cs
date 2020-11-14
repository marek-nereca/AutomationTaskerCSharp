namespace Tasker.Models.Configuration
{
    public class MqttSwitchWithTurnOffDelay : MqttSwitch
    {
        public int TurnOffDelay { get; set; } = 0;
    }
}