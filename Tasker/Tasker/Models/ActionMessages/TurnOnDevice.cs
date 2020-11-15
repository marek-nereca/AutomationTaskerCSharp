using Tasker.Models.Configuration;

namespace Tasker.Models.ActionMessages
{
    public class TurnOnDevice : IActionMessage
    {
        public HueDevice HueDevice { get; }
        
        public TurnOnDevice(HueDevice hueDevice)
        {
            HueDevice = hueDevice;
        }

        public void Process(IActionProcessor processor)
        {
            processor.Accept(this);
        }
    }
}