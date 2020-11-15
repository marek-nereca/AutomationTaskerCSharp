namespace Tasker.Models.Configuration
{
    public class TasmotaRfSwitchWithTurnOffDelay : TasmotaRfSwitch
    {
        public int TurnOffDelay { get; set; } = 0;
        
        public bool OnlyWhenIsDark { get; set; }
        
        public bool OnlyWhenIsNight { get; set; }
    }
}