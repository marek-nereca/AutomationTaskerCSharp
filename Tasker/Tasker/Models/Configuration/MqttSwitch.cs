namespace Tasker.Models.Configuration
{
    public class MqttSwitch
    {
        public string Topic { get; set; }
        
        public HueDevice[] HueDevices { get; set; }
    }
}