namespace StateMachineCore.Core.StateMachine
{
    /// <summary>
    /// Represents a state in the state machine
    /// </summary>
    /// <typeparam name="TContext">The context type that the state operates on</typeparam>
    public interface IState<TContext> where TContext : class
    {
        /// <summary>
        /// Gets the unique identifier for this state
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the display name for this state
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Validates if the transition to this state is allowed
        /// </summary>
        /// <param name="context">The context to validate</param>
        /// <param name="fromState">The state transitioning from</param>
        /// <returns>True if transition is valid, false otherwise</returns>
        Task<bool> CanExcute(TContext context, IState<TContext> fromState);

        /// <summary>
        /// Executes the entry logic when transitioning to this state
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="fromState">The state transitioning from</param>
        /// <returns>True if entry logic succeeded, false otherwise</returns>
        Task<bool> OnEntryAsync(TContext context, IState<TContext> fromState);

        /// <summary>
        /// Executes the rollback logic when fall
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="toState">The state transitioning to</param>
        /// <returns>True if exit logic succeeded, false otherwise</returns>
        //Task<bool> OnExitAsync(TContext context, IState<TContext> toState);
        Task<bool> RollBack(TContext context, IState<TContext> fromState);

    }
} 