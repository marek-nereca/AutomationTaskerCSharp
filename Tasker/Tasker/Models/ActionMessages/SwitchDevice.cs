using Tasker.Models.Configuration;

namespace Tasker.Models.ActionMessages
{
    public class SwitchDevice : IActionMessage
    {
        public HueDevice HueDevice { get; }
        
        public SwitchDevice(HueDevice hueDevice)
        {
            HueDevice = hueDevice;
        }

        public void Process(IActionProcessor processor)
        {
            processor.Accept(this);
        }
    }
}