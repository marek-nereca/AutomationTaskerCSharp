namespace Tasker.Models.Configuration
{
    public class TasmotaRfSwitch
    {
        public string? RfData { get; set; }
        
        public HueDevice[] HueDevices { get; set; } = new HueDevice[0];
    }
}