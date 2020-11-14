namespace Tasker.Models.Configuration
{
    public class DeviceConfig
    {
        public HueBridge[] HueBridges { get; set; }
        public MqttBroker MqttBroker { get; set; }
        
        public HueSensorUpdater HueSensorUpdater { get; set; }
        
        public Switches SimpleSwitches { get; set; }
        
        public TurnOnSwitches OnSwitches { get; set; }
        
        public Switches OffSwitches { get; set; }
    }
}