//using Microsoft.Extensions.Logging;
//using StateMachineCore.Core.StateMachine.Examples;

//namespace StateMachineCore.Core.StateMachine
//{
//    /// <summary>
//    /// Factory implementation for creating state machines
//    /// </summary>
//    public class StateMachineFactory : IStateMachineFactory<IStateContext>
//    {
//        private readonly ILoggerFactory _loggerFactory;
//        private readonly Dictionary<string, Func<ILogger, StateMachine<IStateContext>>> _stateMachineCreators;

//        public StateMachineFactory(ILoggerFactory loggerFactory)
//        {
//            _loggerFactory = loggerFactory;
//            //_stateMachineCreators = new Dictionary<string, Func<ILogger, StateMachine<IStateContext>>>
//            //{
//            //    { "Order", logger => new OrderStateMachine(logger as ILogger<OrderStateMachine>) },
//            //    { "Payment", logger => new PaymentStateMachine(logger as ILogger<PaymentStateMachine>) },
//            //    { "Asset", logger => new AssetStateMachine(logger as ILogger<AssetStateMachine>) },
//            //    { "Settlement", logger => new SettlementStateMachine(logger as ILogger<SettlementStateMachine>) }
//            //};
//        }

//        /// <summary>
//        /// Creates a state machine for the specified context type
//        /// </summary>
//        /// <param name="contextType">The type of context to create a state machine for</param>
//        /// <returns>A configured state machine</returns>
//        public StateMachine<IStateContext> CreateStateMachine(string contextType)
//        {
//            if (string.IsNullOrEmpty(contextType))
//                throw new ArgumentException("Context type cannot be null or empty", nameof(contextType));

//            if (!_stateMachineCreators.TryGetValue(contextType, out var creator))
//            {
//                throw new ArgumentException($"Unsupported context type: {contextType}", nameof(contextType));
//            }

//            var logger = _loggerFactory.CreateLogger(GetLoggerCategory(contextType));
//            var stateMachine = creator(logger);

//            //if (!stateMachine.ValidateConfiguration())
//            //{
//            //    throw new InvalidOperationException($"Invalid state machine configuration for context type: {contextType}");
//            //}

//            return stateMachine;
//        }

//        /// <summary>
//        /// Gets the supported context types
//        /// </summary>
//        /// <returns>List of supported context types</returns>
//        public IEnumerable<string> GetSupportedContextTypes()
//        {
//            return _stateMachineCreators.Keys;
//        }

//        /// <summary>
//        /// Checks if the factory supports the specified context type
//        /// </summary>
//        /// <param name="contextType">The context type to check</param>
//        /// <returns>True if supported, false otherwise</returns>
//        public bool SupportsContextType(string contextType)
//        {
//            return _stateMachineCreators.ContainsKey(contextType);
//        }

//        /// <summary>
//        /// Gets the logger category for the specified context type
//        /// </summary>
//        /// <param name="contextType">The context type</param>
//        /// <returns>The logger category</returns>
//        private string GetLoggerCategory(string contextType)
//        {
//            return $"{contextType}StateMachine";
//        }
//    }

//    /// <summary>
//    /// Settlement state machine implementation
//    /// </summary>
//    public class SettlementStateMachine : StateMachine<SettlementTransaction>
//    {
//        public SettlementStateMachine(ILogger<SettlementStateMachine> logger) : base(logger)
//        {
//            ConfigureStates();
//            ConfigureTransitions();
//        }

//        private void ConfigureStates()
//        {
//            // Add all settlement states
//            AddState(new PendingSettlementState());
//            AddState(new LockedSettlementState());
//            AddState(new ProcessingSettlementState());
//            AddState(new FeeDiscountSettlementState());
//            AddState(new CompletedSettlementState());
//            AddState(new FailedSettlementState());
//        }

//        private void ConfigureTransitions()
//        {
//            // Pending -> Locked
//            AddTransition(new SettlementTransition(
//                "pending_to_locked",
//                "Lock Assets",
//                "Lock assets for settlement",
//                new PendingSettlementState(),
//                new LockedSettlementState(),
//                "lock_assets"
//            ));

//            // Locked -> Processing
//            AddTransition(new SettlementTransition(
//                "locked_to_processing",
//                "Process Transfer",
//                "Process asset transfer",
//                new LockedSettlementState(),
//                new ProcessingSettlementState(),
//                "process_transfer"
//            ));

//            // Processing -> FeeDiscount
//            AddTransition(new SettlementTransition(
//                "processing_to_fee_discount",
//                "Process Fees",
//                "Process trading fees",
//                new ProcessingSettlementState(),
//                new FeeDiscountSettlementState(),
//                "process_fees"
//            ));

//            // FeeDiscount -> Completed
//            AddTransition(new SettlementTransition(
//                "fee_discount_to_completed",
//                "Complete Settlement",
//                "Complete settlement process",
//                new FeeDiscountSettlementState(),
//                new CompletedSettlementState(),
//                "complete"
//            ));

//            // Any -> Failed
//            AddTransition(new SettlementTransition(
//                "any_to_failed",
//                "Fail Settlement",
//                "Settlement failed",
//                new PendingSettlementState(),
//                new FailedSettlementState(),
//                "fail"
//            ));
//        }
//    }

//    #region Settlement States

//    public class PendingSettlementState : IState<SettlementTransaction>
//    {
//        public string Id => "Pending";
//        public string Name => "Pending";
//        public string Description => "Settlement is pending";

//        public Task<bool> CanTransitionToAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnEntryAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnExitAsync(SettlementTransaction context, IState<SettlementTransaction> toState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> ValidateContextAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    public class LockedSettlementState : IState<SettlementTransaction>
//    {
//        public string Id => "Locked";
//        public string Name => "Locked";
//        public string Description => "Assets are locked for settlement";

//        public Task<bool> CanTransitionToAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            return Task.FromResult(fromState.Id == "Pending");
//        }

//        public Task<bool> OnEntryAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnExitAsync(SettlementTransaction context, IState<SettlementTransaction> toState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> ValidateContextAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    public class ProcessingSettlementState : IState<SettlementTransaction>
//    {
//        public string Id => "Processing";
//        public string Name => "Processing";
//        public string Description => "Settlement is being processed";

//        public Task<bool> CanTransitionToAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            return Task.FromResult(fromState.Id == "Locked");
//        }

//        public Task<bool> OnEntryAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnExitAsync(SettlementTransaction context, IState<SettlementTransaction> toState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> ValidateContextAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    public class FeeDiscountSettlementState : IState<SettlementTransaction>
//    {
//        public string Id => "FeeDiscount";
//        public string Name => "Fee Discount";
//        public string Description => "Processing trading fees";

//        public Task<bool> CanTransitionToAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            return Task.FromResult(fromState.Id == "Processing");
//        }

//        public Task<bool> OnEntryAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnExitAsync(SettlementTransaction context, IState<SettlementTransaction> toState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> ValidateContextAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    public class CompletedSettlementState : IState<SettlementTransaction>
//    {
//        public string Id => "Completed";
//        public string Name => "Completed";
//        public string Description => "Settlement completed successfully";

//        public Task<bool> CanTransitionToAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            return Task.FromResult(fromState.Id == "FeeDiscount");
//        }

//        public Task<bool> OnEntryAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnExitAsync(SettlementTransaction context, IState<SettlementTransaction> toState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> ValidateContextAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    public class FailedSettlementState : IState<SettlementTransaction>
//    {
//        public string Id => "Failed";
//        public string Name => "Failed";
//        public string Description => "Settlement failed";

//        public Task<bool> CanTransitionToAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            return Task.FromResult(true); // Can transition from any state to failed
//        }

//        public Task<bool> OnEntryAsync(SettlementTransaction context, IState<SettlementTransaction> fromState)
//        {
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> OnExitAsync(SettlementTransaction context, IState<SettlementTransaction> toState)
//        {
//            return Task.FromResult(true);
//        }

//        public Task<bool> ValidateContextAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    #endregion

//    #region Settlement Transitions

//    public class SettlementTransition : ITransition<SettlementTransaction>
//    {
//        public string Id { get; }
//        public string Name { get; }
//        public string Description { get; }
//        public IState<SettlementTransaction> FromState { get; }
//        public IState<SettlementTransaction> ToState { get; }
//        public string Trigger { get; }

//        public SettlementTransition(string id, string name, string description, 
//            IState<SettlementTransaction> fromState, IState<SettlementTransaction> toState, string trigger)
//        {
//            Id = id;
//            Name = name;
//            Description = description;
//            FromState = fromState;
//            ToState = toState;
//            Trigger = trigger;
//        }

//        public Task<bool> CanExecuteAsync(SettlementTransaction context)
//        {
//            return Task.FromResult(context.CurrentStateId == FromState.Id);
//        }

//        public Task<bool> ExecuteAsync(SettlementTransaction context)
//        {
//            // Settlement-specific transition logic
//            context.UpdatedAt = DateTime.UtcNow;
//            return Task.FromResult(true);
//        }

//        public Task<bool> CheckGuardConditionsAsync(SettlementTransaction context)
//        {
//            // Check if settlement can be transitioned
//            return Task.FromResult(!string.IsNullOrEmpty(context.Id) && 
//                                 !string.IsNullOrEmpty(context.TradeId));
//        }
//    }

//    #endregion
//} 