using Microsoft.Extensions.Logging;
using StateMachineCore.Core.StateMachine;

namespace StateMachineCore.Core.StateMachine.Examples
{
    /// <summary>
    ///   context for demonstration
    /// </summary>
    public class Context : IStateContext
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StepCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // IStateContext implementation
        public string CurrentStateId { get; set; } = "Start";
    }

    /// <summary>
    ///   state machine without transitions
    /// </summary>
    public class StateMachine : StateMachine<Context>
    {
        public StateMachine(ILogger<StateMachine> logger) : base(logger)
        {
            ConfigureStates();
        }

        private void ConfigureStates()
        {
            AddState(new StartState());
            AddState(new ProcessingState());
            AddState(new ValidatingState());
            AddState(new CompletedState());
        }
    }

    #region   States

    public class StartState : IState<Context>
    {
        public string Id => "Start";
        public string Name => "Start";

        public Task<bool> CanExcute(Context context, IState<Context> fromState)
        {
            return Task.FromResult(true);
        }

        public Task<bool> OnEntryAsync(Context context, IState<Context> fromState)
        {
            context.UpdatedAt = DateTime.UtcNow;
            context.StepCount = 0;
            return Task.FromResult(true);
        }

        public Task<bool> RollBack(Context context, IState<Context> fromState)
        {
            throw new NotImplementedException();
        }

    }

    public class ProcessingState : IState<Context>
    {
        public string Id => "Processing";
        public string Name => "Processing";

        public Task<bool> CanExcute(Context context, IState<Context> fromState)
        {
            return Task.FromResult(fromState.Id == "Processing");
        }

        public Task<bool> OnEntryAsync(Context context, IState<Context> fromState)
        {
            context.UpdatedAt = DateTime.UtcNow;
            context.StepCount++;
            return Task.FromResult(true);
        }

        public Task<bool> RollBack(Context context, IState<Context> fromState)
        {
            throw new NotImplementedException();
        }
    }

    public class ValidatingState : IState<Context>
    {
        public string Id => "Validating";
        public string Name => "Validating";

        public Task<bool> CanExcute(Context context, IState<Context> fromState)
        {
            return Task.FromResult(fromState.Id == "Validating");
        }

        public Task<bool> OnEntryAsync(Context context, IState<Context> fromState)
        {
            context.UpdatedAt = DateTime.UtcNow;
            context.StepCount++;
            return Task.FromResult(true);
        }

        public Task<bool> RollBack(Context context, IState<Context> fromState)
        {
            throw new NotImplementedException();
        }
    }

    public class CompletedState : IState<Context>
    {
        public string Id => "Completed";
        public string Name => "Completed";

        public Task<bool> CanExcute(Context context, IState<Context> fromState)
        {
            return Task.FromResult(fromState.Id == "Completed");
        }

        public Task<bool> OnEntryAsync(Context context, IState<Context> fromState)
        {
            context.UpdatedAt = DateTime.UtcNow;
            context.StepCount++;
            return Task.FromResult(true);
        }

        public Task<bool> RollBack(Context context, IState<Context> fromState)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    /// <summary>
    /// Example usage of the   state machine
    /// </summary>
    public class StateMachineExample
    {
        private readonly ILogger<StateMachineExample> _logger;
        private readonly StateMachine _stateMachine;

        public StateMachineExample()
        {
           _logger =  LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<StateMachineExample>();
           var stateLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<StateMachine>();
            _stateMachine = new StateMachine(stateLogger);
        }

        /// <summary>
        /// Demonstrates step-by-step execution without transitions
        /// </summary>
        public async Task DemonstrateStepExecution()
        {
            _logger.LogInformation("===   State Machine Step Execution Demo ===");

            // Create context
            var context = new Context
            {
                Id = "-001",
                Name = "Test Process",
                CreatedAt = DateTime.UtcNow,
                CurrentStateId = "Start"
            };

            _logger.LogInformation("Initial state: {State}", context.CurrentStateId);

            // Define steps to execute (target state IDs)
            var steps = new[]
            {
                "Processing",   // Start -> Processing
                "Validating",   // Processing -> Validating
                "Completed"     // Validating -> Completed
            };

            // Execute all steps
            var success = await _stateMachine.ExecuteStepsAsync(context, steps);

            if (success)
            {
                _logger.LogInformation("All steps completed successfully!");
                _logger.LogInformation("Final state: {State}", context.CurrentStateId);
                _logger.LogInformation("Total steps executed: {StepCount}", context.StepCount);
            }
            else
            {
                _logger.LogError("Step execution failed!");
                _logger.LogInformation("Failed at state: {State}", context.CurrentStateId);
            }
        }

        /// <summary>
        /// Demonstrates individual step execution
        /// </summary>
        public async Task DemonstrateIndividualSteps()
        {
            _logger.LogInformation("=== Individual Step Execution Demo ===");

            var context = new Context
            {
                Id = "-002",
                Name = "Individual Test",
                CreatedAt = DateTime.UtcNow,
                CurrentStateId = "Start"
            };

            // Execute steps one by one
            var step1 = await _stateMachine.ExecuteStepAsync(context, "Processing");
            _logger.LogInformation("Step 1 (Processing): {Success}, State: {State}", 
                step1, context.CurrentStateId);

            var step2 = await _stateMachine.ExecuteStepAsync(context, "Validating");
            _logger.LogInformation("Step 2 (Validating): {Success}, State: {State}", 
                step2, context.CurrentStateId);

            var step3 = await _stateMachine.ExecuteStepAsync(context, "Completed");
            _logger.LogInformation("Step 3 (Completed): {Success}, State: {State}", 
                step3, context.CurrentStateId);

            _logger.LogInformation("Final result: {Success}, Final state: {State}, Steps: {StepCount}", 
                step1 && step2 && step3, context.CurrentStateId, context.StepCount);
        }

        /// <summary>
        /// Demonstrates error handling with steps
        /// </summary>
        public async Task DemonstrateErrorHandling()
        {
            _logger.LogInformation("=== Error Handling Demo ===");

            var context = new Context
            {
                Id = "-003",
                Name = "Error Test",
                CreatedAt = DateTime.UtcNow,
                CurrentStateId = "Start"
            };

            // Try to execute invalid step
            var invalidStep = await _stateMachine.ExecuteStepAsync(context, "InvalidState");
            _logger.LogInformation("Invalid step result: {Success}, State: {State}", 
                invalidStep, context.CurrentStateId);

            // Try to execute steps with invalid order
            var steps = new[] { "Validating", "Processing", "Completed" };
            var stepResult = await _stateMachine.ExecuteStepsAsync(context, steps);
            _logger.LogInformation("Invalid step order result: {Success}, State: {State}", 
                stepResult, context.CurrentStateId);
        }

        /// <summary>
        /// Demonstrates settlement-like workflow
        /// </summary>
        public async Task DemonstrateSettlementWorkflow()
        {
            _logger.LogInformation("=== Settlement Workflow Demo ===");

            var context = new Context
            {
                Id = "SETTLEMENT-001",
                Name = "Settlement Process",
                CreatedAt = DateTime.UtcNow,
                CurrentStateId = "Start"
            };

            // Settlement workflow steps
            var settlementSteps = new[]
            {
                "Processing",   // Start processing
                "Validating",   // Validate data
                "Completed"     // Complete settlement
            };

            var success = await _stateMachine.ExecuteStepsAsync(context, settlementSteps);

            if (success)
            {
                _logger.LogInformation("Settlement workflow completed successfully!");
                _logger.LogInformation("Final state: {State}", context.CurrentStateId);
            }
            else
            {
                _logger.LogError("Settlement workflow failed!");
                _logger.LogInformation("Failed at state: {State}", context.CurrentStateId);
            }
        }
    }
} 