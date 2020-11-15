namespace Tasker.Models.ActionMessages
{
    public interface IActionMessage
    {
        void Process(IActionProcessor processor);
    }
}