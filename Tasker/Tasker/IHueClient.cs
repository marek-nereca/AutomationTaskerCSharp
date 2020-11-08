using System.Threading.Tasks;
using Tasker.Models;

namespace Tasker
{
    public interface IHueClient
    {
        Task TurnDeviceOnAsync(HueDevice device);
        Task TurnDeviceOffAsync(HueDevice device);
        Task SwitchDeviceAsync(HueDevice device);
    }
}