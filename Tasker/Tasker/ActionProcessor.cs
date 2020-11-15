using System.Threading.Tasks;
using Tasker.Models.ActionMessages;

namespace Tasker
{
    public class ActionProcessor : IActionProcessor
    {
        private readonly IHueClient _hueClient;

        public ActionProcessor(IHueClient hueClient)
        {
            _hueClient = hueClient;
        }

        public async Task Accept(SwitchDevice msg)
        {
            await _hueClient.SwitchDeviceAsync(msg.HueDevice);
        }

        public async Task Accept(TurnOffDevice msg)
        {
            await _hueClient.TurnDeviceOffAsync(msg.HueDevice);
        }

        public async Task Accept(TurnOnDevice msg)
        {
            await _hueClient.TurnDeviceOnAsync(msg.HueDevice);
        }
    }
}