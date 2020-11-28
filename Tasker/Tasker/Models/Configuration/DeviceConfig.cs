namespace Tasker.Models.Configuration
{
    public class DeviceConfig
    {
        public HueBridge[] HueBridges { get; set; }
        public MqttBroker MqttBroker { get; set; }
        
        public HueSensorUpdater HueSensorUpdater { get; set; }
        
        public Switches SimpleSwitches { get; set; } = new Switches();
        
        public TurnOnSwitches OnSwitches { get; set; } = new TurnOnSwitches();
        
        public Switches OffSwitches { get; set; } = new Switches();
    }
}