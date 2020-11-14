namespace Tasker.Models.Configuration
{
    public class HueSensorUpdater
    {
        public string? BridgeName { get; set; }
        
        public int DarkSensorId { get; set; }
        
        public int DaySensorId { get; set; }
        
        public int IntervalMs { get; set; }
    }
}