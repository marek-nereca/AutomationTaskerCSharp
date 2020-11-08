namespace Tasker.Models
{
    public class DeviceConfig
    {
        public HueBridge[] HueBridges { get; set; }
        public MqttBroker MqttBroker { get; set; }
        
        public Switches SimpleSwitches { get; set; }
        
        public TurnOnSwitches OnSwitches { get; set; }
        
        public Switches OffSwitches { get; set; }
    }
}