namespace Tasker.Models
{
    public class HueDevice
    {
        public string BridgeName { get; set; }
        public int LightId { get; set; }
    }

    public class HueDeviceGroup
    {
        public HueDevice[] HueDevices { get; set; }
    }
}