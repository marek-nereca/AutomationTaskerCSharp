namespace Tasker.Models
{
    public class MqttSwitch
    {
        public string Topic { get; set; }
        
        public HueDevice[] HueDevices { get; set; }
    }
}