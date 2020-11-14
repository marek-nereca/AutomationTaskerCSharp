namespace Tasker.Models.Configuration
{
    public class TasmotaRfSwitchWithTurnOffDelay : TasmotaRfSwitch
    {
        public int TurnOffDelay { get; set; } = 0;
    }
}