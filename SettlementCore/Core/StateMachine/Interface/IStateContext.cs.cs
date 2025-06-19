namespace StateMachineCore.Core.StateMachine
{
    public interface IStateContext
    {
        string CurrentStateId { get; set; }
    }
}