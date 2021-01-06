namespace Tasker.Models.Configuration
{
    public interface ISwitchWithTurnOffDelay
    {
        public HueDevice[] HueDevices { get; set; }
        
        public int TurnOffDelayMs { get; set; }
        
    }
}