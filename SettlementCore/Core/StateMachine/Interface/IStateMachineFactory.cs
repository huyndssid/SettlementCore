namespace StateMachineCore.Core.StateMachine
{
    /// <summary>
    /// Factory interface for creating state machines
    /// </summary>
    /// <typeparam name="TContext">The context type that the state machine operates on</typeparam>
    public interface IStateMachineFactory<TContext> where TContext : class
    {
        /// <summary>
        /// Creates a state machine for the specified context type
        /// </summary>
        /// <param name="contextType">The type of context to create a state machine for</param>
        /// <returns>A configured state machine</returns>
        StateMachine<TContext> CreateStateMachine(string contextType);

        /// <summary>
        /// Gets the supported context types
        /// </summary>
        /// <returns>List of supported context types</returns>
        IEnumerable<string> GetSupportedContextTypes();

        /// <summary>
        /// Checks if the factory supports the specified context type
        /// </summary>
        /// <param name="contextType">The context type to check</param>
        /// <returns>True if supported, false otherwise</returns>
        bool SupportsContextType(string contextType);
    }
} 