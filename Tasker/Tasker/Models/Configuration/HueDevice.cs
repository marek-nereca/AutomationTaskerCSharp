namespace Tasker.Models.Configuration
{
    public class HueDevice
    {
        public string? BridgeName { get; set; }
        
        public int Id { get; set; }
        
        public bool IsGroup { get; set; }
    }
}