using System.Threading.Tasks;
using Tasker.Models.Configuration;

namespace Tasker
{
    public interface IHueClient
    {
        Task TurnDeviceOnAsync(HueDevice device);
        Task TurnDeviceOffAsync(HueDevice device);
        Task SwitchDeviceAsync(HueDevice device);
    }
}