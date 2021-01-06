namespace Tasker.Models.Configuration
{
    public class TasmotaRfSwitchWithTurnOffDelay : TasmotaRfSwitch, ISwitchWithTurnOffDelay, ISwitchWithNightSelector
    {
        public int TurnOffDelayMs { get; set; }
        public bool OnlyWhenIsDark { get; set; }
        public bool OnlyWhenIsNight { get; set; }
    }
}