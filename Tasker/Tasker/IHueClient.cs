using System.Threading.Tasks;
using Tasker.Models;

namespace Tasker
{
    public interface IHueClient
    {
        Task TurnLightOnAsync(HueDevice device);
        Task TurnLightOffAsync(HueDevice device);
        Task SwitchLightAsync(HueDevice device);
    }
}