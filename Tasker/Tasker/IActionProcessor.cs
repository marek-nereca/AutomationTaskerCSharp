using System.Threading.Tasks;
using Tasker.Models.ActionMessages;

namespace Tasker
{
    public interface IActionProcessor
    {
        Task Accept(SwitchDevice msg);

        Task Accept(TurnOffDevice msg);

        Task Accept(TurnOnDevice msg);
    }
}