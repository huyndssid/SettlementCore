using Microsoft.Extensions.Logging;

namespace StateMachineCore.Core.StateMachine
{
    /// <summary>
    /// Simplified state machine without transitions - uses only states and step execution
    /// </summary>
    /// <typeparam name="TContext">The context type that the state machine operates on</typeparam>
    public class StateMachine<TContext> where TContext : class
    {
        private readonly ILogger<StateMachine<TContext>> _logger;
        private readonly Dictionary<string, IState<TContext>> _states;

        public StateMachine(ILogger<StateMachine<TContext>> logger)
        {
            _logger = logger;
            _states = new Dictionary<string, IState<TContext>>();
        }

        /// <summary>
        /// Gets the current state of the context
        /// </summary>
        /// <param name="context">The context to get the current state for</param>
        /// <returns>The current state, or null if not found</returns>
        public IState<TContext>? GetCurrentState(TContext context)
        {
            if (context is IStateContext stateContext)
            {
                var stateId = stateContext.CurrentStateId;
                return _states.GetValueOrDefault(stateId);
            }
            return null;
        }

        /// <summary>
        /// Adds a state to the state machine
        /// </summary>
        /// <param name="state">The state to add</param>
        public void AddState(IState<TContext> state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            _states[state.Id] = state;
            _logger.LogDebug("Added state: {StateId} - {StateName}", state.Id, state.Name);
        }

        /// <summary>
        /// Executes a step by transitioning to the specified state
        /// </summary>
        /// <param name="context">The context to transition</param>
        /// <param name="targetStateId">The target state ID</param>
        /// <returns>True if transition succeeded, false otherwise</returns>
        public async Task<bool> ExecuteStepAsync(TContext context, string step)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(step))
                throw new ArgumentException("Step state cannot be null or empty", nameof(step));

            var currentState = _states.GetValueOrDefault(step);
            if (currentState == null)
            {
                _logger.LogError("Current state not found for context");
                return false;
            }

            try
            {
                // Check if transition is allowed
                if (!await currentState.CanExcute(context, currentState))
                {
                    _logger.LogWarning("Transition not allowed {State}", currentState.Name);
                    return false;
                }

                // Execute entry logic for new state
                if (!await currentState.OnEntryAsync(context, currentState))
                {
                    _logger.LogError("Entry logic failed for state {StateId}", currentState.Id);
                    return false;
                }

                _logger.LogInformation("Successfully completed stage: {State}", 
                    currentState.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing step to {State}", currentState.Name);
                return false;
            }
        }

        public async void RollbackStep(TContext context, string step)
        {
            var currentState = _states.GetValueOrDefault(step);
            await currentState.RollBack(context, currentState);
        }

        /// <summary>
        /// Executes a sequence of steps (state transitions)
        /// </summary>
        /// <param name="context">The context to transition</param>
        /// <param name="steps">Array of target state IDs to execute</param>
        /// <returns>True if all steps succeeded, false otherwise</returns>
        public async Task<bool> ExecuteStepsAsync(TContext context, string[] steps)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (steps == null || steps.Length == 0)
                throw new ArgumentException("Steps cannot be null or empty", nameof(steps));

            _logger.LogInformation("Starting execution of {StepCount} steps", steps.Length);

            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                _logger.LogInformation("Executing step {StepNumber}/{TotalSteps}: {StepName}", 
                    i + 1, steps.Length, step);

                var success = await ExecuteStepAsync(context, step);
                if (!success)
                {
                    _logger.LogError("Step {StepNumber} failed: {StepName}", i + 1, step);
                    RollbackStep(context, step);
                    return false;
                }

                _logger.LogInformation("Step {StepNumber} completed successfully: {StepName}", 
                    i + 1, step);
            }

            _logger.LogInformation("All {StepCount} steps completed successfully", steps.Length);
            return true;
        }

    }
} 