using Tasker.Models.Configuration;

namespace Tasker.Models.ActionMessages
{
    public class TurnOffDevice : IActionMessage
    {
        public HueDevice HueDevice { get; }
        
        public TurnOffDevice(HueDevice hueDevice)
        {
            HueDevice = hueDevice;
        }

        public void Process(IActionProcessor processor)
        {
            processor.Accept(this);
        }
    }
}