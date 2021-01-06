namespace Tasker.Models.Configuration
{
    public class MqttSwitchWithTurnOffDelay : MqttSwitch, ISwitchWithTurnOffDelay, ISwitchWithNightSelector
    {
        public int TurnOffDelayMs { get; set; }
        public bool OnlyWhenIsDark { get; set; }
        public bool OnlyWhenIsNight { get; set; }
    }
}