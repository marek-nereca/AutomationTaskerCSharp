namespace Tasker.Models
{
    public class HueDevice
    {
        public string BridgeName { get; set; }
        public int Id { get; set; }
        
        public bool IsGroup { get; set; }
    }

    public class HueDeviceGroup
    {
        public HueDevice[] HueDevices { get; set; }
    }
}