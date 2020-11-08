namespace Tasker.Models
{
    public class TasmotaRfSwitch
    {
        public string RfData { get; set; }
        
        public HueDevice[] HueDevices { get; set; }
    }
}